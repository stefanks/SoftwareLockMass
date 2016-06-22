using Chemistry;
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
        public static List<TrainingPoint> GetTrainingPoints(IMsDataFile<IMzSpectrum<MzPeak, MzRange>> myMsDataFile, Identifications identifications, SoftwareLockMassParams p)
        {
            // The final training point list
            List<TrainingPoint> trainingPointsToReturn = new List<TrainingPoint>();

            // Set of peaks, identified by m/z and retention time. If a peak is in here, it means it has been a part of an accepted identification, and should be rejected
            HashSet<Tuple<double, double>> peaksAddedHashSet = new HashSet<Tuple<double, double>>();

            int numIdentifications = identifications.getNumBelow(0);
            // Loop over all identifications
            for (int matchIndex = 0; matchIndex < numIdentifications; matchIndex++)
            {
                // Skip decoys, they are not there!
                if (identifications.isDecoy(matchIndex))
                    continue;

                // Each identification has an MS2 spectrum attached to it. 
                int ms2spectrumIndex = identifications.ms2spectrumIndex(matchIndex);

                // Get the peptide, don't forget to add the modifications!!!!
                Peptide peptideBuilder = new Peptide(identifications.PeptideSequenceWithoutModifications(matchIndex));
                for (int i = 0; i < identifications.NumModifications(matchIndex); i++)
                    peptideBuilder.AddModification(new ChemicalFormulaModification(p.getFormulaFromDictionary(identifications.modificationDictionary(matchIndex, i), identifications.modificationAcession(matchIndex, i))), identifications.modificationLocation(matchIndex, i));
                Peptide peptide = peptideBuilder;

                #region watch
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                    p.OnWatch(new OutputHandlerEventArgs("ms2spectrumIndex: " + ms2spectrumIndex));
                p.OnWatch(new OutputHandlerEventArgs(" calculatedMassToCharge: " + identifications.calculatedMassToCharge(matchIndex)));
                p.OnWatch(new OutputHandlerEventArgs(" experimentalMassToCharge: " + identifications.experimentalMassToCharge(matchIndex)));
                p.OnWatch(new OutputHandlerEventArgs(" Error according to single morpheus point: " + ((identifications.experimentalMassToCharge(matchIndex)) - (identifications.calculatedMassToCharge(matchIndex)))));
                p.OnWatch(new OutputHandlerEventArgs("peptide: " + peptide.Sequence));
                #endregion

                List<TrainingPoint> candidateTrainingPointsForPeptide = new List<TrainingPoint>();

                // Look in the MS2 spectrum for evidence of peptide
                double myMS2score = SearchMS2Spectrum(myMsDataFile.GetScan(ms2spectrumIndex), peptide, candidateTrainingPointsForPeptide, p);

                // Calculate isotopic distribution
                IsotopicDistribution dist = new IsotopicDistribution(p.fineResolution);
                double[] masses;
                double[] intensities;
                dist.CalculateDistribuition(peptideBuilder.GetChemicalFormula(), out masses, out intensities);
                var fullSpectrum = new DefaultMzSpectrum(masses, intensities, false);
                IMzSpectrum<MzPeak, MzRange> distributionSpectrum = fullSpectrum.newSpectrumFilterByNumberOfMostIntense(Math.Min(p.numIsotopologuesToConsider, fullSpectrum.Count));

                List<double> myMS1downScores = SearchMS1Spectra(myMsDataFile, distributionSpectrum, candidateTrainingPointsForPeptide, ms2spectrumIndex, -1, peaksAddedHashSet, p);
                List<double> myMS1upScores = SearchMS1Spectra(myMsDataFile, distributionSpectrum, candidateTrainingPointsForPeptide, ms2spectrumIndex, 1, peaksAddedHashSet, p);

                // Progress
                if (numIdentifications < 100 || matchIndex % (numIdentifications / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs(100 * matchIndex / numIdentifications));

                if (scoresPassed(myMS2score, myMS1downScores, myMS1upScores))
                {
                    trainingPointsToReturn.AddRange(candidateTrainingPointsForPeptide);
                }
            }
            p.OnOutput(new OutputHandlerEventArgs());
            return trainingPointsToReturn;
        }

        private static bool scoresPassed(double myMS2score, List<double> myMS1downScores, List<double> myMS1upScores)
        {
            return true;
        }

        private static double SearchMS2Spectrum(IMsDataScan<IMzSpectrum<MzPeak, MzRange>> ms2DataScan, Peptide peptide, List<TrainingPoint> myCandidatePoints, SoftwareLockMassParams p)
        {
            int ms2spectrumIndex = ms2DataScan.SpectrumNumber;

            var countForThisMS2 = 0;

            var products = peptide.Fragment(FragmentTypes.b | FragmentTypes.y, true);

            var rangeOfSpectrum = ms2DataScan.MzRange;

            #region Generate Theoretical Spectrum

            List<Peak> peaks = new List<Peak>();

            foreach (ChemicalFormulaFragment fragment in products)
            {
                #region watch
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs("  Looking for fragment " + fragment.GetSequence() + " with mass " + fragment.MonoisotopicMass));
                }
                #endregion

                IsotopicDistribution dist = new IsotopicDistribution(p.fineResolution);
                double[] masses;
                double[] intensities;
                dist.CalculateDistribuition(fragment.thisChemicalFormula, out masses, out intensities);
                var fullSpectrum = new DefaultSpectrum(masses, intensities, false);
                var distributionSpectrum = fullSpectrum.newSpectrumFilterByNumberOfMostIntense(Math.Min(p.numIsotopologuesToConsider, fullSpectrum.Count));

                // TODO: Verify that can have charge as low as 1 in the MS2 spectrum
                for (int chargeToLookAt = 1; ; chargeToLookAt++)
                {
                    DefaultSpectrum chargedDistribution = distributionSpectrum.newSpectrumApplyFunctionToX(s => s.ToMz(chargeToLookAt));

                    #region watch
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) && chargedDistribution.GetRange().IsOverlapping(p.mzRange))
                    {
                        p.OnWatch(new OutputHandlerEventArgs("chargedDistribution:" + string.Join(",", chargedDistribution.Select(b => b.X))));
                    }
                    #endregion

                    if (chargedDistribution.FirstX > rangeOfSpectrum.Maximum)
                        continue;
                    if (chargedDistribution.LastX < rangeOfSpectrum.Minimum)
                        break;

                    foreach (Peak peak in chargedDistribution)
                        if (rangeOfSpectrum.Contains(peak.X))
                            peaks.Add(peak);
                }
            }

            #endregion

            DefaultMzSpectrum theoreticalSpectrum = new DefaultMzSpectrum(peaks.Select(b => b.X).ToArray(), peaks.Select(b => b.Y).ToArray(), false);

            #region Match Theoretical Spectrum To Actual

            int i = 0;
            countForThisMS2 = 0;
            foreach (Peak peak in theoreticalSpectrum)
            {
                #region watch
                if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex)) && p.mzRange.Contains(peak.X))
                {
                    p.OnWatch(new OutputHandlerEventArgs("   Looking for " + peak.X));
                }
                #endregion

                while (i < ms2DataScan.MassSpectrum.Count && ms2DataScan.MassSpectrum.GetX(i) < peak.X - p.toleranceInMZforSearch)
                    i++;
                if (i == ms2DataScan.MassSpectrum.Count)
                    break;
                if (ms2DataScan.MassSpectrum.GetX(i) < peak.X + p.toleranceInMZforSearch)
                {
                    #region watch
                    if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex)) && p.mzRange.Contains(peak.X))
                    {
                        p.OnWatch(new OutputHandlerEventArgs("   Found       " + ms2DataScan.MassSpectrum.GetX(i) + "   Error is    " + (ms2DataScan.MassSpectrum.GetX(i) - peak.X)));
                    }
                    #endregion

                    myCandidatePoints.Add(new TrainingPoint(new DataPoint(ms2DataScan.MassSpectrum.GetX(i), ms2DataScan.RetentionTime), ms2DataScan.MassSpectrum.GetX(i) - peak.X));
                    countForThisMS2++;
                }
            }

            #endregion

            #region watch
            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
            {
                p.OnWatch(new OutputHandlerEventArgs("  countForThisMS2  =   " + countForThisMS2));
            }
            #endregion

            return countForThisMS2;
        }

        private static List<double> SearchMS1Spectra(IMsDataFile<IMzSpectrum<MzPeak, MzRange>> myMsDataFile, IMzSpectrum<MzPeak, MzRange> distributionSpectrum, List<TrainingPoint> myCandidatePoints, int ms2spectrumIndex, int direction, HashSet<Tuple<double, double>> peaksAddedHashSet, SoftwareLockMassParams p)
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
                // Console.WriteLine("In loop!");
                // Console.WriteLine("theIndex = " + theIndex);
                if (myMsDataFile.GetScan(theIndex).MsnOrder > 1)
                {
                    theIndex += direction;
                    continue;
                }
                //// Console.WriteLine("aaaaa");
                added = false;
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs(" Looking in MS1 spectrum " + theIndex + " because of MS2 spectrum " + ms2spectrumIndex));
                }

                //// Console.WriteLine("bbbbb");
                var fullMS1spectrum = myMsDataFile.GetScan(theIndex);
                //// Console.WriteLine("ccccc");
                double ms1RetentionTime = fullMS1spectrum.RetentionTime;
                //// Console.WriteLine("ddddd");
                var rangeOfSpectrum = fullMS1spectrum.MzRange;
                //// Console.WriteLine("eeeee");
                var ok1 = fullMS1spectrum;
                //// Console.WriteLine("fffff");
                var ok2 = ok1.MassSpectrum;
                //// Console.WriteLine("gggggg");
                var ms1FilteredByHighIntensities = ok2.newSpectrumFilterByY(p.intensityCutoff, double.MaxValue);
                //// Console.WriteLine("hhhhhh");
                if (ms1FilteredByHighIntensities.Count() == 0)
                {
                    theIndex += direction;
                    continue;
                }
                for (int chargeToLookAt = 1; ; chargeToLookAt++)
                {
                    // Console.WriteLine("chargeToLookAt = " + chargeToLookAt);
                    ISpectrum<MzPeak, MzRange> chargedDistribution = distributionSpectrum.newSpectrumApplyFunctionToX(s => (s + chargeToLookAt * Constants.Proton) / chargeToLookAt);

                    if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && chargedDistribution.GetRange().IsOverlapping(p.mzRange))
                    {
                        p.OnWatch(new OutputHandlerEventArgs("  chargedDistribution: " + string.Join(", ", chargedDistribution.Take(4)) + "..."));
                    }

                    // Console.WriteLine("  chargedDistribution.LastMZ = " + chargedDistribution.LastMZ);
                    // Console.WriteLine("  rangeOfSpectrum.Maximum = " + rangeOfSpectrum.Maximum);
                    if (chargedDistribution.FirstX > rangeOfSpectrum.Maximum)
                        continue;
                    if (chargedDistribution.LastX < rangeOfSpectrum.Minimum)
                        break;

                    // Console.WriteLine("  Seriously looking at this charge, it's within range");

                    List<TrainingPoint> trainingPointsToAverage = new List<TrainingPoint>();
                    for (int isotopologueIndex = 0; isotopologueIndex < Math.Min(p.numIsotopologuesToConsider, chargedDistribution.Count); isotopologueIndex++)
                    {
                        var closestPeak = ms1FilteredByHighIntensities.GetClosestPeak(chargedDistribution[isotopologueIndex].MZ);
                        var theTuple = Tuple.Create<double, double>(closestPeak.X, ms1RetentionTime);
                        if (Math.Abs(chargedDistribution[isotopologueIndex].MZ - closestPeak.X) < p.toleranceInMZforSearch && !peaksAddedHashSet.Contains(theTuple))
                        {
                            // Console.WriteLine("  Added!");
                            peaksAddedHashSet.Add(theTuple);

                            if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && chargedDistribution.GetRange().IsOverlapping(p.mzRange))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("   Looking for " + chargedDistribution[isotopologueIndex].MZ + "   Found       " + closestPeak.X + "   Error is    " + (closestPeak.X - chargedDistribution[isotopologueIndex].MZ)));
                            }
                            trainingPointsToAverage.Add(new TrainingPoint(new DataPoint(closestPeak.X, ms1RetentionTime), closestPeak.X - chargedDistribution[isotopologueIndex].MZ));
                        }
                        else
                            break;
                    }
                    if (trainingPointsToAverage.Count >= p.numIsotopologuesNeededToBeConsideredIdentified)
                    {
                        added = true;
                        // Hack! Last isotopologue seems to be troublesome, often has error
                        trainingPointsToAverage.RemoveAt(trainingPointsToAverage.Count - 1);
                        var a = new TrainingPoint(new DataPoint(trainingPointsToAverage.Select(b => b.dp.mz).Average(), trainingPointsToAverage.Select(b => b.dp.rt).Average()), trainingPointsToAverage.Select(b => b.l).Median());

                        if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && chargedDistribution.GetRange().IsOverlapping(p.mzRange))
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
