﻿using Chemistry;
using MassSpectrometry;
using MathNet.Numerics.Statistics;
using Proteomics;
using Spectra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    class TrainingPointsExtractor
    {
        private static double toleranceInMZforSearch = 0;
        public static List<TrainingPoint> GetTrainingPoints(IMsDataFile<IMzSpectrum<MzPeak, MzRange>> myMsDataFile, Identifications identifications, SoftwareLockMassParams p)
        {
            // The final training point list
            List<TrainingPoint> trainingPointsToReturn = new List<TrainingPoint>();

            // Set of peaks, identified by m/z and retention time. If a peak is in here, it means it has been a part of an accepted identification, and should be rejected
            HashSet<Tuple<double, double>> peaksAddedHashSet = new HashSet<Tuple<double, double>>();

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

                List<TrainingPoint> candidateTrainingPointsForPeptide = new List<TrainingPoint>();

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

                List<double> myMS1downScores = SearchMS1Spectra(myMsDataFile, masses, intensities, candidateTrainingPointsForPeptide, ms2spectrumIndex, -1, peaksAddedHashSet, p);
                List<double> myMS1upScores = SearchMS1Spectra(myMsDataFile, masses, intensities, candidateTrainingPointsForPeptide, ms2spectrumIndex, 1, peaksAddedHashSet, p);

                if (scoresPassed(numFragmentsIdentified, myMS1downScores, myMS1upScores))
                {
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                    {
                        p.OnOutput(new OutputHandlerEventArgs(" myMS2score = " + numFragmentsIdentified));
                    }
                    trainingPointsToReturn.AddRange(candidateTrainingPointsForPeptide);
                }
            }
            p.OnOutput(new OutputHandlerEventArgs("Number of training points: " + trainingPointsToReturn.Count()));
            return trainingPointsToReturn;
        }

        private static bool scoresPassed(double myMS2score, List<double> myMS1downScores, List<double> myMS1upScores)
        {
            return true;
        }

        private static int SearchMS2Spectrum(IMsDataScan<IMzSpectrum<MzPeak, MzRange>> ms2DataScan, Peptide peptide, int peptideCharge, List<TrainingPoint> myCandidatePoints, SoftwareLockMassParams p)
        {
            int ms2spectrumIndex = ms2DataScan.SpectrumNumber;

            var countForThisMS2 = 0;
            var countForThisMS2a = 0;
            var numFragmentsIdentified = 0;

            var rangeOfSpectrum = ms2DataScan.MzRange;

            var ms2mzArray = ms2DataScan.MassSpectrum.xArray;

            Fragment[] fragmentList = peptide.Fragment(FragmentTypes.b | FragmentTypes.y, true).ToArray();

            #region One time tolerance calculation

            if (toleranceInMZforSearch == 0)
            {
                List<double> myList = new List<double>();
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    Console.WriteLine(" Considering individual fragments for tolerance calculation:");
                }
                foreach (IHasMass fragment in fragmentList)
                {
                    // First look for monoisotopic masses, do not compute distribution spectrum!
                    for (int chargeToLookAt = 1; ; chargeToLookAt++)
                    {
                        var monoisotopicMZ = fragment.MonoisotopicMass.ToMassToChargeRatio(chargeToLookAt);
                        if (monoisotopicMZ > rangeOfSpectrum.Maximum)
                            continue;
                        if (monoisotopicMZ < rangeOfSpectrum.Minimum)
                            break;
                        var closestPeakMZ = ms2DataScan.MassSpectrum.GetClosestPeakXvalue(monoisotopicMZ);
                        if (Math.Abs(closestPeakMZ - monoisotopicMZ) < 1)
                            myList.Add(closestPeakMZ - monoisotopicMZ);
                    }
                }

                myList.Sort();

                toleranceInMZforSearch = getTolerance(myList.ToArray());

                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    Console.WriteLine(" Final tolerance: " + toleranceInMZforSearch);
                }
                // double tolerance = getToleranceFromMixtureData(myList);
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
                for (int chargeToLookAt = 1; chargeToLookAt <= peptideCharge; chargeToLookAt++)
                {
                    var monoisotopicMZ = fragment.MonoisotopicMass.ToMassToChargeRatio(chargeToLookAt);
                    if (monoisotopicMZ > rangeOfSpectrum.Maximum)
                        continue;
                    if (monoisotopicMZ < rangeOfSpectrum.Minimum)
                        break;
                    var closestPeakMZ = ms2DataScan.MassSpectrum.GetClosestPeakXvalue(monoisotopicMZ);
                    if (Math.Abs(closestPeakMZ - monoisotopicMZ) < toleranceInMZforSearch)
                    {
                        if (!computedIsotopologues)
                        {
                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                Console.WriteLine("    Computing isotopologues because error " + (closestPeakMZ - monoisotopicMZ) + " is smaller than tolerance " + toleranceInMZforSearch);
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

                if (computedIsotopologues)
                {
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                    {
                        Console.WriteLine("   Considering individual charges, to get training points:");
                    }
                    bool startingToAdd = false;
                    for (int chargeToLookAt = 1; chargeToLookAt <= peptideCharge && 0.5 > toleranceInMZforSearch * chargeToLookAt; chargeToLookAt++)
                    {
                        if (masses.First().ToMassToChargeRatio(chargeToLookAt) > rangeOfSpectrum.Maximum)
                            continue;
                        if (masses.Last().ToMassToChargeRatio(chargeToLookAt) < rangeOfSpectrum.Minimum)
                            break;
                        if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                        {
                            Console.WriteLine("    Considering charge " + chargeToLookAt);
                        }
                        List<TrainingPoint> trainingPointsToAverage = new List<TrainingPoint>();
                        foreach (double a in masses)
                        {
                            double theMZ = a.ToMassToChargeRatio(chargeToLookAt);
                            var closestPeakMZ = ms2DataScan.MassSpectrum.GetClosestPeakXvalue(theMZ);
                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("      Looking for " + theMZ));
                            }
                            if (Math.Abs(closestPeakMZ - theMZ) < toleranceInMZforSearch)
                            {
                                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                                {
                                    p.OnWatch(new OutputHandlerEventArgs("      Found       " + closestPeakMZ + "   Error is    " + (closestPeakMZ - theMZ)));
                                }
                                trainingPointsToAverage.Add(new TrainingPoint(new DataPoint(closestPeakMZ, ms2DataScan.RetentionTime), closestPeakMZ - theMZ));
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
                            // Hack! Last isotopologue seems to be troublesome, often has error
                            var a = new TrainingPoint(new DataPoint(trainingPointsToAverage.Select(b => b.dp.mz).Average(), trainingPointsToAverage.Select(b => b.dp.rt).Average()), trainingPointsToAverage.Select(b => b.l).Median());

                            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("    Adding aggregate of " + trainingPointsToAverage.Count + " points FROM MS2 SPECTRUM"));
                                p.OnWatch(new OutputHandlerEventArgs("    a.dp.mz " + a.dp.mz));
                                p.OnWatch(new OutputHandlerEventArgs("    a.dp.rt " + a.dp.rt));
                                p.OnWatch(new OutputHandlerEventArgs("    a.l     " + a.l));
                            }
                            myCandidatePoints.Add(a);
                        }
                    }
                }
            }

            //p.OnWatch(new OutputHandlerEventArgs("ind = " + ms2spectrumIndex + " count = " + countForThisMS2));
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

        private static List<double> SearchMS1Spectra(IMsDataFile<IMzSpectrum<MzPeak, MzRange>> myMsDataFile, double[] originalMasses, double[] originalIntensities, List<TrainingPoint> myCandidatePoints, int ms2spectrumIndex, int direction, HashSet<Tuple<double, double>> peaksAddedHashSet, SoftwareLockMassParams p)
        {
            var theIndex = -1;
            if (direction == 1)
                theIndex = ms2spectrumIndex;
            else
                theIndex = ms2spectrumIndex - 1;

            bool added = true;

            List<double> scores = new List<double>();
            while (theIndex >= myMsDataFile.FirstSpectrumNumber && theIndex <= myMsDataFile.LastSpectrumNumber && added == true)
            {
                if (myMsDataFile.GetScan(theIndex).MsnOrder > 1)
                {
                    theIndex += direction;
                    continue;
                }
                added = false;
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs(" Looking in MS1 spectrum " + theIndex + " because of MS2 spectrum " + ms2spectrumIndex));
                }
                var fullMS1scan = myMsDataFile.GetScan(theIndex);
                double ms1RetentionTime = fullMS1scan.RetentionTime;
                var rangeOfSpectrum = fullMS1scan.MzRange;
                var fullMS1spectrum = fullMS1scan.MassSpectrum;
                if (fullMS1spectrum.Count == 0)
                    break;
                for (int chargeToLookAt = 1; ; chargeToLookAt++)
                {
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex))
                    {
                        p.OnWatch(new OutputHandlerEventArgs(" Looking at charge " + chargeToLookAt));
                    }
                    if (originalMasses[0].ToMassToChargeRatio(chargeToLookAt) > rangeOfSpectrum.Maximum)
                        continue;
                    if (originalMasses[0].ToMassToChargeRatio(chargeToLookAt) < rangeOfSpectrum.Minimum)
                        break;
                    List<TrainingPoint> trainingPointsToAverage = new List<TrainingPoint>();
                    for (int isotopologueIndex = 0; isotopologueIndex < originalMasses.Count(); isotopologueIndex++)
                    {
                        var closestPeak = fullMS1spectrum.GetClosestPeak(originalMasses[isotopologueIndex].ToMassToChargeRatio(chargeToLookAt));
                        var theTuple = Tuple.Create<double, double>(closestPeak.X, ms1RetentionTime);
                        if (Math.Abs(originalMasses[isotopologueIndex].ToMassToChargeRatio(chargeToLookAt) - closestPeak.X) < toleranceInMZforSearch && !peaksAddedHashSet.Contains(theTuple))
                        {
                            // Console.WriteLine("  Added!");
                            peaksAddedHashSet.Add(theTuple);

                            if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && p.mzRange.Contains(originalMasses[isotopologueIndex].ToMassToChargeRatio(chargeToLookAt)))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("   Looking for " + originalMasses[isotopologueIndex].ToMassToChargeRatio(chargeToLookAt) + "   Found       " + closestPeak.X + "   Error is    " + (closestPeak.X - originalMasses[isotopologueIndex].ToMassToChargeRatio(chargeToLookAt))));
                            }
                            trainingPointsToAverage.Add(new TrainingPoint(new DataPoint(closestPeak.X, ms1RetentionTime), closestPeak.X - originalMasses[isotopologueIndex].ToMassToChargeRatio(chargeToLookAt)));
                        }
                        else
                            break;
                    }
                    if (trainingPointsToAverage.Count >= 3)
                    {
                        added = true;
                        // Hack! Last isotopologue seems to be troublesome, often has error
                        trainingPointsToAverage.RemoveAt(trainingPointsToAverage.Count - 1);
                        var a = new TrainingPoint(new DataPoint(trainingPointsToAverage.Select(b => b.dp.mz).Average(), trainingPointsToAverage.Select(b => b.dp.rt).Average()), trainingPointsToAverage.Select(b => b.l).Median());

                        if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && p.mzRange.Contains(originalMasses[0].ToMassToChargeRatio(chargeToLookAt)))
                        {
                            p.OnWatch(new OutputHandlerEventArgs("  Adding aggregate of " + trainingPointsToAverage.Count + " points"));
                            p.OnWatch(new OutputHandlerEventArgs("  a.dp.mz " + a.dp.mz));
                            p.OnWatch(new OutputHandlerEventArgs("  a.dp.rt " + a.dp.rt));
                            p.OnWatch(new OutputHandlerEventArgs("  a.l     " + a.l));
                            p.OnWatch(new OutputHandlerEventArgs());
                        }
                        scores.Add(trainingPointsToAverage.Count);
                        myCandidatePoints.Add(a);
                    }
                }
                theIndex += direction;
            }
            return scores;
        }
    }
}
