﻿using Chemistry;
using MassSpectrometry;
using MathNet.Numerics.Statistics;
using Proteomics;
using Spectra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mzCal
{
    class TrainingPointsExtractor
    {
        private static double toleranceInMZforMS2Search = 0;
        public static List<LabeledDataPoint> GetTrainingPoints(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, Identifications identifications, SoftwareLockMassParams p)
        {
            // The final training point list
            List<LabeledDataPoint> trainingPointsToReturn = new List<LabeledDataPoint>();

            // Set of peaks, identified by m/z and retention time. If a peak is in here, it means it has been a part of an accepted identification, and should be rejected
            HashSet<Tuple<double, double>> peaksAddedFromMS1HashSet = new HashSet<Tuple<double, double>>();

            int numIdentifications = identifications.Count();
            // Loop over all identifications
            for (int matchIndex = 0; matchIndex < numIdentifications; matchIndex++)
            {
                // Progress
                if (numIdentifications < 100 || matchIndex % (numIdentifications / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs(100 * matchIndex / numIdentifications));

                // Skip decoys, they are for sure not there!
                if (identifications.isDecoy(matchIndex))
                    continue;

                // Each identification has an MS2 spectrum attached to it. 
                int ms2spectrumIndex = identifications.ms2spectrumIndex(matchIndex);

                // Get the peptide, don't forget to add the modifications!!!!
                Peptide peptideBuilder = new Peptide(identifications.PeptideSequenceWithoutModifications(matchIndex));
                for (int i = 0; i < identifications.NumModifications(matchIndex); i++)
                    peptideBuilder.AddModification(new ChemicalFormulaModification(p.getFormulaFromDictionary(identifications.modificationDictionary(matchIndex, i), identifications.modificationAcession(matchIndex, i))), identifications.modificationLocation(matchIndex, i));
                Peptide peptide = peptideBuilder;
                int peptideCharge = identifications.chargeState(matchIndex);

                #region watch
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs("ms2spectrumIndex: " + ms2spectrumIndex));
                    p.OnWatch(new OutputHandlerEventArgs(" calculatedMassToCharge: " + identifications.calculatedMassToCharge(matchIndex)));
                    p.OnWatch(new OutputHandlerEventArgs(" experimentalMassToCharge: " + identifications.experimentalMassToCharge(matchIndex)));
                    p.OnWatch(new OutputHandlerEventArgs(" Error according to single morpheus point: " + ((identifications.experimentalMassToCharge(matchIndex)) - (identifications.calculatedMassToCharge(matchIndex)))));
                    p.OnWatch(new OutputHandlerEventArgs(" peptide: " + peptide.GetSequenceWithModifications()));
                }
                #endregion

                List<LabeledDataPoint> candidateTrainingPointsForPeptide = new List<LabeledDataPoint>();

                // Look in the MS2 spectrum for evidence of peptide
                int numFragmentsIdentified = SearchMS2Spectrum(myMsDataFile.GetScan(ms2spectrumIndex), peptide, peptideCharge, candidateTrainingPointsForPeptide, p);


                // If MS2 has low evidence for peptide, skip and go to next one
                if (numFragmentsIdentified < 9)
                    continue;

                // Calculate isotopic distribution of the full peptide
                IsotopicDistribution dist = new IsotopicDistribution(peptideBuilder.GetChemicalFormula(), p.fineResolution, 0.001);

                double[] masses = new double[dist.Masses.Count];
                double[] intensities = new double[dist.Intensities.Count];
                for (int i = 0; i < dist.Masses.Count; i++)
                {
                    masses[i] = dist.Masses[i];
                    intensities[i] = dist.Intensities[i];
                }
                Array.Sort(intensities, masses, Comparer<double>.Create((x, y) => y.CompareTo(x)));

                List<int> myMS1downScores = SearchMS1Spectra(myMsDataFile, masses, intensities, candidateTrainingPointsForPeptide, ms2spectrumIndex, -1, peaksAddedFromMS1HashSet, p, peptideCharge);
                List<int> myMS1upScores = SearchMS1Spectra(myMsDataFile, masses, intensities, candidateTrainingPointsForPeptide, ms2spectrumIndex, 1, peaksAddedFromMS1HashSet, p, peptideCharge);

                if (scoresPassed(numFragmentsIdentified, myMS1downScores, myMS1upScores))
                {
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                    {
                        p.OnWatch(new OutputHandlerEventArgs(" myMS2score = " + numFragmentsIdentified));
                    }
                    trainingPointsToReturn.AddRange(candidateTrainingPointsForPeptide);
                }
            }
            p.OnOutput(new OutputHandlerEventArgs(""));
            p.OnOutput(new OutputHandlerEventArgs("Number of training points: " + trainingPointsToReturn.Count()));
            return trainingPointsToReturn;
        }

        private static bool scoresPassed(double myMS2score, List<int> myMS1downScores, List<int> myMS1upScores)
        {
            if (myMS2score > 9)
                return true;
            return false;
        }

        private static int SearchMS2Spectrum(IMsDataScan<IMzSpectrum<MzPeak>> ms2DataScan, Peptide peptide, int peptideCharge, List<LabeledDataPoint> myCandidatePoints, SoftwareLockMassParams p)
        {
            int SelectedIonGuessChargeStateGuess;
            ms2DataScan.TryGetSelectedIonGuessChargeStateGuess(out SelectedIonGuessChargeStateGuess);
            double IsolationMZ;
            ms2DataScan.TryGetIsolationMZ(out IsolationMZ);
            var TotalIonCurrent = ms2DataScan.TotalIonCurrent;
            var InjectionTime = ms2DataScan.InjectionTime;
            var ScanWindowRange = ms2DataScan.ScanWindowRange;

            int ms2spectrumIndex = ms2DataScan.ScanNumber;

            var countForThisMS2 = 0;
            var countForThisMS2a = 0;
            var numFragmentsIdentified = 0;

            var scanWindowRange = ms2DataScan.ScanWindowRange;

            var ms2mzArray = ms2DataScan.MassSpectrum.xArray;

            Fragment[] fragmentList = peptide.Fragment(FragmentTypes.b | FragmentTypes.y, true).ToArray();

            #region One time tolerance calculation

            if (toleranceInMZforMS2Search == 0)
            {
                List<double> myList = new List<double>();
                foreach (IHasMass fragment in fragmentList)
                {
                    // First look for monoisotopic masses, do not compute distribution spectrum!
                    for (int chargeToLookAt = 1; ; chargeToLookAt++)
                    {
                        var monoisotopicMZ = fragment.MonoisotopicMass.ToMassToChargeRatio(chargeToLookAt);
                        if (monoisotopicMZ > scanWindowRange.Maximum)
                            continue;
                        if (monoisotopicMZ < scanWindowRange.Minimum)
                            break;
                        var closestPeakMZ = ms2DataScan.MassSpectrum.GetClosestPeakXvalue(monoisotopicMZ);
                        if (Math.Abs(closestPeakMZ - monoisotopicMZ) < 1)
                            myList.Add(closestPeakMZ - monoisotopicMZ);
                    }
                }

                myList.Sort();

                toleranceInMZforMS2Search = getTolerance(myList.ToArray());

            }
            #endregion

            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
            {
                Console.WriteLine(" Considering individual fragments:");
            }
            foreach (IHasChemicalFormula fragment in fragmentList)
            {
                bool fragmentIdentified = false;
                bool computedIsotopologues = false;
                double[] masses = new double[0];
                double[] intensities = new double[0];
                // First look for monoisotopic masses, do not compute distribution spectrum!
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    Console.WriteLine("  Considering fragment " + (fragment as Fragment).Sequence + " with formula " + fragment.ThisChemicalFormula.Formula);
                    //if ((fragment as Fragment).Modifications.Count() > 0)
                    //Console.WriteLine("  Modifications: " + string.Join(", ", (fragment as Fragment).Modifications));
                }

                #region loop to determine if need to compute isotopologue distribution
                for (int chargeToLookAt = 1; chargeToLookAt <= peptideCharge; chargeToLookAt++)
                {
                    var monoisotopicMZ = fragment.MonoisotopicMass.ToMassToChargeRatio(chargeToLookAt);
                    if (monoisotopicMZ > scanWindowRange.Maximum)
                        continue;
                    if (monoisotopicMZ < scanWindowRange.Minimum)
                        break;
                    var closestPeakMZ = ms2DataScan.MassSpectrum.GetClosestPeakXvalue(monoisotopicMZ);
                    if (Math.Abs(closestPeakMZ - monoisotopicMZ) < toleranceInMZforMS2Search)
                    {
                        if (!computedIsotopologues)
                        {
                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                Console.WriteLine("    Computing isotopologues because error " + Math.Abs(closestPeakMZ - monoisotopicMZ) + " is smaller than tolerance " + toleranceInMZforMS2Search);
                                Console.WriteLine("    chargeToLookAt = " + chargeToLookAt + "  closestPeakMZ = " + closestPeakMZ + " while monoisotopicMZ = " + monoisotopicMZ);
                            }

                            IsotopicDistribution dist = new IsotopicDistribution(fragment.ThisChemicalFormula, p.fineResolution, 0.001);

                            masses = new double[dist.Masses.Count];
                            intensities = new double[dist.Intensities.Count];
                            for (int i = 0; i < dist.Masses.Count; i++)
                            {
                                masses[i] = dist.Masses[i];
                                intensities[i] = dist.Intensities[i];
                            }
                            Array.Sort(intensities, masses, Comparer<double>.Create((x, y) => y.CompareTo(x)));
                            computedIsotopologues = true;
                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                Console.WriteLine("    Isotopologue distribution: ");
                                Console.WriteLine("    masses = " + string.Join(", ", masses) + "...");
                                Console.WriteLine("    intensities = " + string.Join(", ", intensities) + "...");
                            }

                            break;
                        }
                    }
                }

                #endregion

                if (computedIsotopologues)
                {
                    #region actually add training points
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                    {
                        Console.WriteLine("   Considering individual charges, to get training points:");
                    }
                    bool startingToAdd = false;
                    for (int chargeToLookAt = 1; chargeToLookAt <= peptideCharge && 0.5 > toleranceInMZforMS2Search * chargeToLookAt; chargeToLookAt++)
                    {
                        if (masses.First().ToMassToChargeRatio(chargeToLookAt) > scanWindowRange.Maximum)
                            continue;
                        if (masses.Last().ToMassToChargeRatio(chargeToLookAt) < scanWindowRange.Minimum)
                            break;
                        if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                        {
                            Console.WriteLine("    Considering charge " + chargeToLookAt);
                        }
                        List<TrainingPoint> trainingPointsToAverage = new List<TrainingPoint>();
                        foreach (double a in masses)
                        {
                            double theMZ = a.ToMassToChargeRatio(chargeToLookAt);
                            var closestPeak = ms2DataScan.MassSpectrum.GetClosestPeak(theMZ);
                            var closestPeakMZ = closestPeak.MZ;
                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("      Looking for " + theMZ));
                            }
                            if (Math.Abs(closestPeakMZ - theMZ) < toleranceInMZforMS2Search)
                            {
                                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                                {
                                    p.OnWatch(new OutputHandlerEventArgs("      Found       " + closestPeakMZ + "   Error is    " + (closestPeakMZ - theMZ)));
                                }
                                trainingPointsToAverage.Add(new TrainingPoint(new DataPoint(closestPeakMZ, double.NaN, 0, closestPeak.Intensity, double.NaN, double.NaN), closestPeakMZ - theMZ));
                            }
                            else
                                break;
                        }
                        // If started adding and suddnely stopped, go to next one, no need to look at higher charges
                        if (trainingPointsToAverage.Count == 0 && startingToAdd == true)
                            break;
                        if (trainingPointsToAverage.Count == 1 && intensities[0] < 0.65)
                        {
                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("    Not adding, since intensities[0] is " + intensities[0] + " which is too low"));
                            }
                        }
                        else if (trainingPointsToAverage.Count > 0)
                        {
                            startingToAdd = true;
                            if (!fragmentIdentified)
                            {
                                fragmentIdentified = true;
                                numFragmentsIdentified += 1;
                            }

                            countForThisMS2 += trainingPointsToAverage.Count;
                            countForThisMS2a += 1;

                            double addedMZ = trainingPointsToAverage.Select(b => b.dp.mz).Average();
                            double relativeMZ = (addedMZ - ms2DataScan.ScanWindowRange.Minimum) / (ms2DataScan.ScanWindowRange.Maximum - ms2DataScan.ScanWindowRange.Minimum);
                            double[] inputs = new double[9] { 2, addedMZ, ms2DataScan.RetentionTime, trainingPointsToAverage.Select(b => b.dp.intensity).Average(), ms2DataScan.TotalIonCurrent, ms2DataScan.InjectionTime, SelectedIonGuessChargeStateGuess, IsolationMZ, relativeMZ };
                            var a = new LabeledDataPoint(inputs, trainingPointsToAverage.Select(b => b.l).Median());

                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("    Adding aggregate of " + trainingPointsToAverage.Count + " points FROM MS2 SPECTRUM"));
                                p.OnWatch(new OutputHandlerEventArgs("    a.dp.mz " + a.inputs[1]));
                                p.OnWatch(new OutputHandlerEventArgs("    a.dp.rt " + a.inputs[2]));
                                p.OnWatch(new OutputHandlerEventArgs("    a.l     " + a.output));
                            }
                            myCandidatePoints.Add(a);
                        }
                    }
                    #endregion
                }
            }

            //p.OnWatch(new OutputHandlerEventArgs("ind = " + ms2spectrumIndex + " count = " + countForThisMS2 + " count2 = " + countForThisMS2a + " fragments = " + numFragmentsIdentified));
            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
            {
                p.OnWatch(new OutputHandlerEventArgs(" countForThisMS2 = " + countForThisMS2));
                p.OnWatch(new OutputHandlerEventArgs(" countForThisMS2a = " + countForThisMS2a));
                p.OnWatch(new OutputHandlerEventArgs(" numFragmentsIdentified = " + numFragmentsIdentified));
            }
            return numFragmentsIdentified;
        }

        private static double getTolerance(double[] sortedList)
        {
            var tolerance = 1.0 / 3;
            var trialIndex = Array.BinarySearch(sortedList, 0);
            int indexOfZero = trialIndex >= 0 ? trialIndex : ~trialIndex;
            double oldRatio = 1;
            while (true)
            {
                int countGood = 0;
                int countBadUp = 0;
                int countBadDown = 0;
                // TODO: Replace with binary search
                for (int i = indexOfZero; i < sortedList.Count(); i++)
                {
                    if (sortedList[i] <= tolerance)
                        countGood++;
                    else if (sortedList[i] <= tolerance * 3)
                        countBadUp++;
                }
                for (int i = indexOfZero - 1; i >= 0; i--)
                {
                    if (sortedList[i] >= -tolerance)
                        countGood++;
                    else if (sortedList[i] >= -tolerance * 3)
                        countBadDown++;
                }
                double newRatio = Math.Max(countBadDown / (double)countGood, countBadUp / (double)countGood);
                if (newRatio < oldRatio || newRatio < 0.1)
                {
                    oldRatio = newRatio * 1.5;
                    tolerance /= 2;
                }
                else
                {
                    break;
                }
            }
            return tolerance * 2;
        }

        private static List<int> SearchMS1Spectra(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, double[] originalMasses, double[] originalIntensities, List<LabeledDataPoint> myCandidatePoints, int ms2spectrumIndex, int direction, HashSet<Tuple<double, double>> peaksAddedHashSet, SoftwareLockMassParams p, int peptideCharge)
        {
            List<int> scores = new List<int>();
            var theIndex = -1;
            if (direction == 1)
                theIndex = ms2spectrumIndex;
            else
                theIndex = ms2spectrumIndex - 1;

            bool addedAscan = true;

            int highestKnownChargeForThisPeptide = peptideCharge;
            while (theIndex >= myMsDataFile.FirstSpectrumNumber && theIndex <= myMsDataFile.LastSpectrumNumber && addedAscan == true)
            {
                int countForThisScan = 0;
                if (myMsDataFile.GetScan(theIndex).MsnOrder > 1)
                {
                    theIndex += direction;
                    continue;
                }
                addedAscan = false;
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs(" Looking in MS1 spectrum " + theIndex + " because of MS2 spectrum " + ms2spectrumIndex));
                }
                var fullMS1scan = myMsDataFile.GetScan(theIndex);
                double ms1RetentionTime = fullMS1scan.RetentionTime;
                var scanWindowRange = fullMS1scan.ScanWindowRange;
                var fullMS1spectrum = fullMS1scan.MassSpectrum;
                if (fullMS1spectrum.Count == 0)
                    break;
                bool startingToAddCharges = false;
                int chargeToLookAt = 1;
                do
                {
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex))
                    {
                        p.OnWatch(new OutputHandlerEventArgs("  Looking at charge " + chargeToLookAt));
                    }
                    if (originalMasses[0].ToMassToChargeRatio(chargeToLookAt) > scanWindowRange.Maximum)
                    {
                        chargeToLookAt++;
                        continue;
                    }
                    if (originalMasses[0].ToMassToChargeRatio(chargeToLookAt) < scanWindowRange.Minimum)
                        break;
                    List<TrainingPoint> trainingPointsToAverage = new List<TrainingPoint>();
                    foreach (double a in originalMasses)
                    {
                        double theMZ = a.ToMassToChargeRatio(chargeToLookAt);
                        var closestPeak = fullMS1spectrum.GetClosestPeak(theMZ);
                        var closestPeakMZ = closestPeak.MZ;

                        var theTuple = Tuple.Create<double, double>(closestPeakMZ, ms1RetentionTime);
                        if (Math.Abs(closestPeakMZ - theMZ) < toleranceInMZforMS2Search && !peaksAddedHashSet.Contains(theTuple))
                        {
                            peaksAddedHashSet.Add(theTuple);
                            highestKnownChargeForThisPeptide = Math.Max(highestKnownChargeForThisPeptide, chargeToLookAt);
                            if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && p.mzRange.Contains(theMZ))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("      Found       " + closestPeakMZ + "   Error is    " + (closestPeakMZ - theMZ)));
                            }
                            trainingPointsToAverage.Add(new TrainingPoint(new DataPoint(closestPeakMZ, double.NaN, 1, closestPeak.Intensity, double.NaN, double.NaN), closestPeakMZ - theMZ));
                        }
                        else
                            break;
                    }
                    // If started adding and suddnely stopped, go to next one, no need to look at higher charges
                    if (trainingPointsToAverage.Count == 0 && startingToAddCharges == true)
                        break;
                    if (trainingPointsToAverage.Count == 1 && originalIntensities[0] < 0.65)
                    {
                        if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && p.mzRange.Contains(originalMasses[0].ToMassToChargeRatio(chargeToLookAt)))
                        {
                            p.OnWatch(new OutputHandlerEventArgs("    Not adding, since originalIntensities[0] is " + originalIntensities[0] + " which is too low"));
                        }
                    }
                    else if (trainingPointsToAverage.Count > 0)
                    {
                        addedAscan = true;
                        startingToAddCharges = true;
                        countForThisScan += 1;
                        double[] inputs = new double[6] { 1, trainingPointsToAverage.Select(b => b.dp.mz).Average(), fullMS1scan.RetentionTime,trainingPointsToAverage.Select(b => b.dp.intensity).Average(), fullMS1scan.TotalIonCurrent, fullMS1scan.InjectionTime };
                        var a = new LabeledDataPoint(inputs, trainingPointsToAverage.Select(b => b.l).Median());
                        if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex))
                        {
                            p.OnWatch(new OutputHandlerEventArgs("    Adding aggregate of " + trainingPointsToAverage.Count + " points FROM MS1 SPECTRUM"));
                            p.OnWatch(new OutputHandlerEventArgs("    a.dp.mz " + a.inputs[1]));
                            p.OnWatch(new OutputHandlerEventArgs("    a.dp.rt " + a.inputs[2]));
                            p.OnWatch(new OutputHandlerEventArgs("    a.l     " + a.output));
                        }
                        myCandidatePoints.Add(a);
                    }
                    chargeToLookAt++;
                } while (chargeToLookAt <= highestKnownChargeForThisPeptide + 1);


                theIndex += direction;
                scores.Add(countForThisScan);
            }
            return scores;
        }
    }
}
