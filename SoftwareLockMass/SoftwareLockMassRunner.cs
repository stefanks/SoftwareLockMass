using Chemistry;
using MassSpectrometry;
using MathNet.Numerics.Statistics;
using Proteomics;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SoftwareLockMass
{
    public static class SoftwareLockMassRunner
    {
        public static void Run(SoftwareLockMassParams p)
        {
            p.OnOutput(new OutputHandlerEventArgs("Welcome to my software lock mass implementation"));
            p.OnOutput(new OutputHandlerEventArgs("Calibrating " + Path.GetFileName(p.myMsDataFile.FilePath)));

            p.myMsDataFile.Open();

            p.OnOutput(new OutputHandlerEventArgs("Getting Training Points"));
            List<TrainingPoint> trainingPoints = GetTrainingPoints(p.myMsDataFile, p.identifications, p);

            //p.OnOutput(new OutputHandlerEventArgs("Writing training points to file"));
            //WriteTrainingDataToFiles(trainingPoints);

            p.OnOutput(new OutputHandlerEventArgs("Train the calibration model"));
            CalibrationFunction cf;
            //CalibrationFunction cf = new IdentityCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new ConstantCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new LinearCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new CubicCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new QuarticCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new CalibrationFunctionClustering(p.OnOutput, 20);
            //CalibrationFunction cf = new MedianCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new KDTreeCalibrationFunction(p.OnOutput);
            try
            {
                cf = new QuarticCalibrationFunction(p.OnOutput);
                cf.Train(trainingPoints);
            }
            catch (ArgumentException)
            {
                try
                {
                    cf = new CubicCalibrationFunction(p.OnOutput);
                    cf.Train(trainingPoints);
                }
                catch (ArgumentException)
                {
                    try
                    {
                        cf = new QuadraticCalibrationFunction(p.OnOutput);
                        cf.Train(trainingPoints);
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            cf = new LinearCalibrationFunction(p.OnOutput);
                            cf.Train(trainingPoints);
                        }
                        catch (ArgumentException)
                        {
                            cf = new ConstantCalibrationFunction(p.OnOutput);
                            cf.Train(trainingPoints);
                        }
                    }
                }
            }

            if (p.tsvFile != null)
            {
                p.OnOutput(new OutputHandlerEventArgs("Calibrating TSV file"));
                CalibrateTSV(cf, p);
            }
            p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
            List<IMzSpectrum<MzPeak>> calibratedSpectra = CalibrateSpectra(cf, p);
            p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
            List<double> calibratedPrecursorMZs = CalibratePrecursorMZs(cf, p);

            p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs);

            p.OnOutput(new OutputHandlerEventArgs("Finished running my software lock mass implementation"));
        }

        private static void CalibrateTSV(CalibrationFunction cf, SoftwareLockMassParams p)
        {
            using (StreamReader reader = new StreamReader(p.tsvFile))
            using (StreamWriter file = new StreamWriter(p.tsvFile+".calibrated"))
            {
                string line;
                int lines = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (lines > 0)
                    {
                        // Do something with the line.
                        string[] parts = line.Split('\t');

                        // Needed for getting RT for precursor
                        int MS2spectrumNumber = Convert.ToInt32(parts[1]);
                        var precursorID = p.myMsDataFile.GetScan(MS2spectrumNumber).PrecursorID;
                        int MS1spectrumNumber = MS2spectrumNumber - 1;
                        while (!precursorID.Equals(p.myMsDataFile.GetScan(MS1spectrumNumber).id))
                            MS1spectrumNumber--;

                        double precursorRT = p.myMsDataFile.GetScan(MS1spectrumNumber).RetentionTime;

                        // To Calibrate
                        double precursorMZ = Convert.ToDouble(parts[5]);

                        // To Recompute
                        double precursorMass = Convert.ToDouble(parts[8]);
                        double precursorMassErrorDA = Convert.ToDouble(parts[18]);
                        double precursorMassErrorppm = Convert.ToDouble(parts[19]);

                        // Other needed info
                        int precursorCharge = Convert.ToInt32(parts[7]);
                        double theoreticalMass = Convert.ToDouble(parts[17]);


                        precursorMZ -= cf.Predict(new DataPoint(precursorMZ, precursorRT));
                        precursorMass = precursorMZ.ToMass(precursorCharge);
                        precursorMassErrorDA = precursorMass - theoreticalMass;
                        precursorMassErrorppm = precursorMassErrorDA / theoreticalMass * 1e6;

                        parts[5] = precursorMZ.ToString();
                        parts[8] = precursorMass.ToString();
                        parts[18] = precursorMassErrorDA.ToString();
                        parts[19] = precursorMassErrorppm.ToString();

                        file.WriteLine(string.Join("\t", parts));
                    }
                    else
                        file.WriteLine(line);
                    lines++;
                }
            }
        }

        private static void WriteTrainingDataToFiles(List<TrainingPoint> trainingPoints)
        {
            using (StreamWriter file = new StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData1.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.mz);
            using (StreamWriter file = new StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData2.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.rt);
            using (StreamWriter file = new StreamWriter(@"E:\Stefan\data\CalibratedOutput\labelData.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.l);
        }

        private static List<TrainingPoint> GetTrainingPoints(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, Identifications identifications, SoftwareLockMassParams p)
        {
            // Get the training data out of xml
            List<TrainingPoint> trainingPointsToReturn = new List<TrainingPoint>();

            HashSet<Tuple<double, double>> peaksAddedHashSet = new HashSet<Tuple<double, double>>();

            int numPass = identifications.getNumBelow(p.thresholdPassParameter);
            // Loop over all results from the mzIdentML file
            for (int matchIndex = 0; matchIndex < numPass; matchIndex++)
            {
                //p.OnOutput(new OutputHandlerEventArgs((matchIndex);
                if (numPass < 100)
                    p.OnProgress(new ProgressHandlerEventArgs(numPass));
                else if (matchIndex % (numPass / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs(matchIndex / (numPass / 100)));
                if (identifications.isDecoy(matchIndex))
                    continue;

                string ms2spectrumID = identifications.spectrumID(matchIndex);
                int ms2spectrumIndex = GetLastNumberFromString(ms2spectrumID);
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs("ms2spectrumIndex: " + ms2spectrumIndex));
                    p.OnWatch(new OutputHandlerEventArgs(" calculatedMassToCharge: " + identifications.calculatedMassToCharge(matchIndex)));
                    p.OnWatch(new OutputHandlerEventArgs(" experimentalMassToCharge: " + identifications.experimentalMassToCharge(matchIndex)));
                    p.OnWatch(new OutputHandlerEventArgs(" Error according to single morpheus point: " + ((identifications.experimentalMassToCharge(matchIndex)) - (identifications.calculatedMassToCharge(matchIndex)))));
                }
                IMzSpectrum<MzPeak> distributionSpectrum;
                int chargeStateFromMorpheus = identifications.chargeState(matchIndex);

                if (p.MZID_MASS_DATA)
                {
                    double calculatedMassToCharge = identifications.calculatedMassToCharge(matchIndex);
                    distributionSpectrum = new DefaultMzSpectrum(new double[1] { calculatedMassToCharge * chargeStateFromMorpheus - chargeStateFromMorpheus * Constants.Proton }, new double[1] { 1 });
                }
                else
                {
                    // Get the peptide, don't forget to add the modifications!!!!
                    Peptide peptide1 = new Peptide(identifications.PeptideSequence(matchIndex));
                    for (int i = 0; i < identifications.NumModifications(matchIndex); i++)
                    {
                        var ok = p.getFormulaFromDictionary(identifications.modificationDictionary(matchIndex, i), identifications.modificationAcession(matchIndex, i));
                        peptide1.AddModification(new ChemicalFormulaModification(ok), identifications.modificationLocation(matchIndex, i));
                    }
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                        p.OnWatch(new OutputHandlerEventArgs("peptide1: " + peptide1.Sequence));

                    // SEARCH THE MS2 SPECTRUM!!!
                    //SearchMS2Spectrum(myMsDataFile, ms2spectrumIndex, peptide1, trainingPointsToReturn, chargeStateFromMorpheus);
                    // Calculate isotopic distribution
                    IsotopicDistribution dist = new IsotopicDistribution(p.fineResolution);
                    double[] masses;
                    double[] intensities;
                    dist.CalculateDistribuition(peptide1.GetChemicalFormula(), out masses, out intensities);
                    var fullSpectrum = new DefaultMzSpectrum(masses, intensities, false);
                    distributionSpectrum = fullSpectrum.FilterByNumberOfMostIntense(Math.Min(p.numIsotopologuesToConsider, fullSpectrum.Count));

                }
                SearchMS1Spectra(myMsDataFile, distributionSpectrum, trainingPointsToReturn, ms2spectrumIndex, -1, peaksAddedHashSet, p);
                SearchMS1Spectra(myMsDataFile, distributionSpectrum, trainingPointsToReturn, ms2spectrumIndex, 1, peaksAddedHashSet, p);
            }
            p.OnOutput(new OutputHandlerEventArgs());
            return trainingPointsToReturn;
        }

        private static void SearchMS2Spectrum(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, int ms2spectrumIndex, Peptide peptide, List<TrainingPoint> trainingPointsToReturn, int chargeStateFromMorpheus, SoftwareLockMassParams p)
        {
            var countForThisMS2 = 0;
            var msDataScan = myMsDataFile.GetScan(ms2spectrumIndex);

            var products = peptide.Fragment(FragmentTypes.b | FragmentTypes.y, true).ToArray();

            var rangeOfSpectrum = msDataScan.MzRange;

            foreach (ChemicalFormulaFragment fragment in products)
            {
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs("  Looking for fragment with mass " + fragment.MonoisotopicMass));
                }

                IsotopicDistribution dist = new IsotopicDistribution(p.fineResolution);
                double[] masses;
                double[] intensities;
                dist.CalculateDistribuition(fragment.thisChemicalFormula, out masses, out intensities);
                var fullSpectrum = new DefaultMzSpectrum(masses, intensities, false);
                var distributionSpectrum = fullSpectrum.FilterByNumberOfMostIntense(Math.Min(p.numIsotopologuesToConsider, fullSpectrum.Count));


                for (int chargeToLookAt = 1; chargeToLookAt <= chargeStateFromMorpheus; chargeToLookAt++)
                {

                    IMzSpectrum<MzPeak> chargedDistribution = distributionSpectrum.CorrectMasses(s => (s + chargeToLookAt * Constants.Proton) / chargeToLookAt);
                    if (p.MS2spectraToWatch.Contains(ms2spectrumIndex) && chargedDistribution.GetMzRange().IsOverlapping(p.mzRange))
                    {
                        p.OnWatch(new OutputHandlerEventArgs("chargedDistribution:" + string.Join(",", chargedDistribution.Select(b => b.MZ))));
                    }
                    if (chargedDistribution.LastMZ > rangeOfSpectrum.Maximum)
                        continue;
                    if (chargedDistribution.GetBasePeak().MZ < rangeOfSpectrum.Minimum)
                        break;

                    List<TrainingPoint> trainingPointsToAverage = new List<TrainingPoint>();
                    for (int isotopologueIndex = 0; isotopologueIndex < Math.Min(p.numIsotopologuesToConsider, chargedDistribution.Count); isotopologueIndex++)
                    {
                        var closestPeak = msDataScan.MassSpectrum.GetClosestPeak(chargedDistribution[isotopologueIndex].MZ);
                        if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex)) && chargedDistribution.GetMzRange().IsOverlapping(p.mzRange))
                        {
                            p.OnWatch(new OutputHandlerEventArgs("   Looking for " + chargedDistribution[isotopologueIndex].MZ + "   Found       " + closestPeak.X + "   Error is    " + (closestPeak.X - chargedDistribution[isotopologueIndex].MZ)));
                        }
                        if (Math.Abs(chargedDistribution[isotopologueIndex].MZ - closestPeak.X) < p.toleranceInMZforSearch)
                        {
                            if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex)) && chargedDistribution.GetMzRange().IsOverlapping(p.mzRange))
                            {
                                p.OnWatch(new OutputHandlerEventArgs("   Adding!"));
                            }
                            trainingPointsToAverage.Add(new TrainingPoint(new DataPoint(closestPeak.X, msDataScan.RetentionTime), closestPeak.X - chargedDistribution[isotopologueIndex].MZ));
                        }
                        else
                            break;
                    }
                    if (trainingPointsToAverage.Count >= p.numIsotopologuesNeededToBeConsideredIdentified)
                    {
                        countForThisMS2 += trainingPointsToAverage.Count - 1;
                        // Hack! Last isotopologue seems to be troublesome, often has error
                        trainingPointsToAverage.RemoveAt(trainingPointsToAverage.Count - 1);
                        var a = new TrainingPoint(new DataPoint(trainingPointsToAverage.Select(b => b.dp.mz).Average(), trainingPointsToAverage.Select(b => b.dp.rt).Average()), trainingPointsToAverage.Select(b => b.l).Median());

                        if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                        {
                            p.OnWatch(new OutputHandlerEventArgs("  Adding aggregate of " + trainingPointsToAverage.Count + " points FROM MS2 SPECTRUM"));
                            p.OnWatch(new OutputHandlerEventArgs("  a.dp.mz " + a.dp.mz));
                            p.OnWatch(new OutputHandlerEventArgs("  a.dp.rt " + a.dp.rt));
                            p.OnWatch(new OutputHandlerEventArgs("  a.l     " + a.l));
                            p.OnWatch(new OutputHandlerEventArgs());
                        }
                        trainingPointsToReturn.Add(a);
                    }

                }
            }
            // Caclulate MS2 distribution
            if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
            {
                p.OnWatch(new OutputHandlerEventArgs("  countForThisMS2  =   " + countForThisMS2));
            }
        }

        private static void SearchMS1Spectra(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, IMzSpectrum<MzPeak> distributionSpectrum, List<TrainingPoint> trainingPointsToReturn, int ms2spectrumIndex, int direction, HashSet<Tuple<double, double>> peaksAddedHashSet, SoftwareLockMassParams p)
        {
            var theIndex = -1;
            if (direction == 1)
                theIndex = ms2spectrumIndex;
            else
                theIndex = ms2spectrumIndex - 1;

            bool added = true;

            // Below should go in a loop!
            // Console.WriteLine("Before loop start! theIndex = " + theIndex);
            // Console.WriteLine("myMsDataFile.FirstSpectrumNumber = " + myMsDataFile.FirstSpectrumNumber + " myMsDataFile.LastSpectrumNumber = " + myMsDataFile.LastSpectrumNumber);
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
                var ms1FilteredByHighIntensities = ok2.FilterByIntensity(p.intensityCutoff, double.MaxValue);
                //// Console.WriteLine("hhhhhh");
                if (ms1FilteredByHighIntensities.Count == 0)
                {
                    theIndex += direction;
                    continue;
                }
                for (int chargeToLookAt = 1; ; chargeToLookAt++)
                {
                    // Console.WriteLine("chargeToLookAt = " + chargeToLookAt);
                    IMzSpectrum<MzPeak> chargedDistribution = distributionSpectrum.CorrectMasses(s => (s + chargeToLookAt * Constants.Proton) / chargeToLookAt);

                    if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && chargedDistribution.GetMzRange().IsOverlapping(p.mzRange))
                    {
                        p.OnWatch(new OutputHandlerEventArgs("  chargedDistribution: " + string.Join(", ", chargedDistribution.Take(4)) + "..."));
                    }

                    // Console.WriteLine("  chargedDistribution.LastMZ = " + chargedDistribution.LastMZ);
                    // Console.WriteLine("  rangeOfSpectrum.Maximum = " + rangeOfSpectrum.Maximum);
                    if (chargedDistribution.FirstMZ > rangeOfSpectrum.Maximum)
                        continue;
                    if (chargedDistribution.LastMZ < rangeOfSpectrum.Minimum)
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

                            if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && chargedDistribution.GetMzRange().IsOverlapping(p.mzRange))
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

                        if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && chargedDistribution.GetMzRange().IsOverlapping(p.mzRange))
                        {
                            p.OnWatch(new OutputHandlerEventArgs("  Adding aggregate of " + trainingPointsToAverage.Count + " points"));
                            p.OnWatch(new OutputHandlerEventArgs("  a.dp.mz " + a.dp.mz));
                            p.OnWatch(new OutputHandlerEventArgs("  a.dp.rt " + a.dp.rt));
                            p.OnWatch(new OutputHandlerEventArgs("  a.l     " + a.l));
                            p.OnWatch(new OutputHandlerEventArgs());
                        }
                        trainingPointsToReturn.Add(a);
                    }
                }
                theIndex += direction;
            }
        }

        private static List<IMzSpectrum<MzPeak>> CalibrateSpectra(CalibrationFunction cf, SoftwareLockMassParams p)
        {
            List<IMzSpectrum<MzPeak>> calibratedSpectra = new List<IMzSpectrum<MzPeak>>();
            for (int i = 0; i < p.myMsDataFile.LastSpectrumNumber; i++)
            {
                if (p.myMsDataFile.LastSpectrumNumber < 100)
                    p.OnProgress(new ProgressHandlerEventArgs(i));
                else if (i % (p.myMsDataFile.LastSpectrumNumber / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs((i / (p.myMsDataFile.LastSpectrumNumber / 100))));
                if (p.MS1spectraToWatch.Contains(i + 1))
                {
                    p.OnWatch(new OutputHandlerEventArgs("Before calibration of spectrum " + (i + 1)));
                    var mzs = p.myMsDataFile.GetSpectrum(i + 1).Extract(p.mzRange);
                    p.OnWatch(new OutputHandlerEventArgs(string.Join(", ", mzs)));
                }
                calibratedSpectra.Add(p.myMsDataFile.GetSpectrum(i + 1).CorrectMasses(s => s - cf.Predict(new DataPoint(s, p.myMsDataFile.GetScan(i + 1).RetentionTime))));
                if (p.MS1spectraToWatch.Contains(i + 1))
                {
                    p.OnWatch(new OutputHandlerEventArgs("After calibration of spectrum " + (i + 1)));
                    var mzs = calibratedSpectra.Last().Extract(p.mzRange);
                    p.OnWatch(new OutputHandlerEventArgs(string.Join(", ", mzs)));
                }

            }
            p.OnOutput(new OutputHandlerEventArgs());
            return calibratedSpectra;
        }

        private static List<double> CalibratePrecursorMZs(CalibrationFunction cf, SoftwareLockMassParams p)
        {
            List<double> calibratedPrecursorMZs = new List<double>();
            double precursorTime = -1;
            for (int i = 0; i < p.myMsDataFile.LastSpectrumNumber; i++)
            {
                if (p.myMsDataFile.LastSpectrumNumber < 100)
                    p.OnProgress(new ProgressHandlerEventArgs(i));
                else if (i % (p.myMsDataFile.LastSpectrumNumber / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs((i / (p.myMsDataFile.LastSpectrumNumber / 100))));
                double newMZ = -1;
                if (p.myMsDataFile.GetScan(i + 1).MsnOrder == 1)
                {
                    precursorTime = p.myMsDataFile.GetScan(i + 1).RetentionTime;
                }
                else
                {
                    newMZ = p.myMsDataFile.GetScan(i + 1).SelectedIonMonoisotopicMZ - cf.Predict(new DataPoint(p.myMsDataFile.GetScan(i + 1).SelectedIonMonoisotopicMZ, precursorTime));
                }
                calibratedPrecursorMZs.Add(newMZ);
            }
            p.OnOutput(new OutputHandlerEventArgs());
            return calibratedPrecursorMZs;
        }

        private static int GetLastNumberFromString(string s)
        {
            return Convert.ToInt32(Regex.Match(s, @"\d+$").Value);
        }
    }

}