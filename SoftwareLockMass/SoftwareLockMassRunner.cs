using Chemistry;
using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using MathNet.Numerics.Statistics;
using Proteomics;
using Spectra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SoftwareLockMass
{
    public static class SoftwareLockMassRunner
    {
        public static SoftwareLockMassParams p;

        public static void Run()
        {
            p.OnOutput(new OutputHandlerEventArgs("Welcome to my software lock mass implementation"));

            foreach (AnEntry anEntry in p.myListOfEntries)
            {
                IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile;
                if (Path.GetExtension(anEntry.spectraFile).Equals(".mzML"))
                {
                    myMsDataFile = new Mzml(anEntry.spectraFile);
                }
                else
                {
                    myMsDataFile = new ThermoRawFile(anEntry.spectraFile);
                }
                
                myMsDataFile.Open();

                p.OnOutput(new OutputHandlerEventArgs("Getting Training Points"));
                List<TrainingPoint> trainingPoints = GetTrainingPoints(myMsDataFile, anEntry.mzidFile);

                //p.OnOutput(new OutputHandlerEventArgs("Writing training points to file"));
                //WriteTrainingDataToFiles(trainingPoints);

                p.OnOutput(new OutputHandlerEventArgs("Train the calibration model"));
                //CalibrationFunction cf = new IdentityCalibrationFunction(p.OnOutput);
                //CalibrationFunction cf = new ConstantCalibrationFunction(p.OnOutput);
                //CalibrationFunction cf = new LinearCalibrationFunction(p.OnOutput);
                //CalibrationFunction cf = new QuadraticCalibrationFunction(p.OnOutput);
                //CalibrationFunction cf = new CubicCalibrationFunction(p.OnOutput);
                CalibrationFunction cf = new QuarticCalibrationFunction(p.OnOutput);
                //CalibrationFunction cf = new CalibrationFunctionClustering(p.OnOutput, 20);
                //CalibrationFunction cf = new MedianCalibrationFunction(p.OnOutput);
                //CalibrationFunction cf = new KDTreeCalibrationFunction(p.OnOutput);
                cf.Train(trainingPoints);

                p.OnOutput(new OutputHandlerEventArgs("Computing Mean Squared Error"));
                p.OnOutput(new OutputHandlerEventArgs("The Mean Squared Error for the model is " + cf.getMSE(trainingPoints)));

                p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                List<IMzSpectrum<MzPeak>> calibratedSpectra = CalibrateSpectra(myMsDataFile, cf);
                p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                List<double> calibratedPrecursorMZs = CalibratePrecursorMZs(myMsDataFile, cf);

                p.OnOutput(new OutputHandlerEventArgs("Creating _indexedmzMLConnection, and putting data in it"));
                indexedmzML _indexedmzMLConnection = CreateMyIndexedMZmlwithCalibratedSpectra(myMsDataFile, calibratedSpectra, calibratedPrecursorMZs);

                p.OnOutput(new OutputHandlerEventArgs("Writing calibrated mzML file"));
                Mzml.Write(Path.GetFileNameWithoutExtension(anEntry.spectraFile)+"-Calibrated.mzML", _indexedmzMLConnection);

            }
            p.OnOutput(new OutputHandlerEventArgs("Finished running my software lock mass implementation"));
            Console.Read();
        }

        private static void WriteTrainingDataToFiles(List<TrainingPoint> trainingPoints)
        {
            using (StreamWriter file =
                new StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData1.dat"))
            {
                foreach (TrainingPoint d in trainingPoints)
                {
                    file.WriteLine(d.dp.mz);
                }
            }

            using (StreamWriter file =
                new StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData2.dat"))
            {
                foreach (TrainingPoint d in trainingPoints)
                {
                    file.WriteLine(d.dp.rt);
                }
            }

            using (StreamWriter file =
                new StreamWriter(@"E:\Stefan\data\CalibratedOutput\labelData.dat"))
            {
                foreach (TrainingPoint d in trainingPoints)
                {
                    file.WriteLine(d.l);
                }
            }
        }

        private static List<TrainingPoint> GetTrainingPoints(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, string mzidFile)
        {
            XmlSerializer _indexedSerializer = new XmlSerializer(typeof(mzIdentML.MzIdentMLType));
            Stream stream = new FileStream(mzidFile, FileMode.Open);
            // Read the XML file into the variable
            mzIdentML.MzIdentMLType dd = _indexedSerializer.Deserialize(stream) as mzIdentML.MzIdentMLType;

            // Get the training data out of xml
            List<TrainingPoint> trainingPointsToReturn = new List<TrainingPoint>();

            UsefulProteomicsDatabases.unimod unimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadUnimod();
            UsefulProteomicsDatabases.obo psimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadPsiMod();

            HashSet<Tuple<double, double>> peaksAddedHashSet = new HashSet<Tuple<double, double>>();

            var aaaaa = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult.Select(b => Convert.ToDouble(b.SpectrumIdentificationItem[0].cvParam[1].value));
            var bbbbb = aaaaa.Where(b => b <= p.thresholdPassParameter);
            int numPass = bbbbb.Count();
            // Loop over all results from the mzIdentML file
            for (int matchIndex = 0; matchIndex < numPass; matchIndex++)
            {
                //p.OnOutput(new OutputHandlerEventArgs((matchIndex);
                if (matchIndex % (numPass / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs(matchIndex / (numPass / 100)));
                if (dd.SequenceCollection.PeptideEvidence[matchIndex].isDecoy)
                    continue;

                string ms2spectrumID = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].spectrumID;

                int ms2spectrumIndex = GetLastNumberFromString(ms2spectrumID);
                if (p.MS2spectraToWatch.Contains(ms2spectrumIndex))
                {
                    p.OnWatch(new OutputHandlerEventArgs("ms2spectrumIndex: " + ms2spectrumIndex));
                    p.OnWatch(new OutputHandlerEventArgs(" calculatedMassToCharge: " + dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge));
                    p.OnWatch(new OutputHandlerEventArgs(" experimentalMassToCharge: " + dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].experimentalMassToCharge));
                    p.OnWatch(new OutputHandlerEventArgs(" Error according to single morpheus point: " + ((dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].experimentalMassToCharge) - (dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge))));
                }
                IMzSpectrum<MzPeak> distributionSpectrum;
                int chargeStateFromMorpheus = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].chargeState;

                if (p.MZID_MASS_DATA)
                {
                    double calculatedMassToCharge = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge;
                    distributionSpectrum = new DefaultMzSpectrum(new double[1] { calculatedMassToCharge * chargeStateFromMorpheus - chargeStateFromMorpheus * Constants.Proton }, new double[1] { 1 });
                }
                else
                {
                    // Get the peptide, don't forget to add the modifications!!!!
                    Peptide peptide1 = new Peptide(dd.SequenceCollection.Peptide[matchIndex].PeptideSequence);
                    if (dd.SequenceCollection.Peptide[matchIndex].Modification != null)
                    {
                        for (int i = 0; i < dd.SequenceCollection.Peptide[matchIndex].Modification.Length; i++)
                        {
                            int location = dd.SequenceCollection.Peptide[matchIndex].Modification[i].location;
                            string theFormula = null;
                            if (dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef == "UNIMOD")
                            {
                                string unimodAcession = dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].accession;
                                var indexToLookFor = GetLastNumberFromString(unimodAcession) - 1;
                                while (unimodDeserialized.modifications[indexToLookFor].record_id != GetLastNumberFromString(unimodAcession))
                                    indexToLookFor--;
                                theFormula = unimodDeserialized.modifications[indexToLookFor].composition;
                            }
                            else if (dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef == "PSI-MOD")
                            {
                                string psimodAcession = dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].accession;
                                UsefulProteomicsDatabases.oboTerm ksadklfj = (UsefulProteomicsDatabases.oboTerm)psimodDeserialized.Items[GetLastNumberFromString(psimodAcession) + 2];
                                if (GetLastNumberFromString(psimodAcession) != GetLastNumberFromString(ksadklfj.id))
                                    throw new Exception("Error in reading psi-mod file");
                                else
                                {
                                    foreach (var a in ksadklfj.xref_analog)
                                    {
                                        if (a.dbname == "DiffFormula")
                                        {
                                            theFormula = a.name;
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                                throw new Exception("Not familiar with modification dictionary " + dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef);
                            ChemicalFormulaModification modification = new ChemicalFormulaModification(ConvertToCSMSLFormula(theFormula));
                            peptide1.AddModification(modification, location);
                        }
                    }
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
                SearchMS1Spectra(myMsDataFile, distributionSpectrum, trainingPointsToReturn, ms2spectrumIndex, -1, peaksAddedHashSet);
                SearchMS1Spectra(myMsDataFile, distributionSpectrum, trainingPointsToReturn, ms2spectrumIndex, 1, peaksAddedHashSet);
            }
            p.OnOutput(new OutputHandlerEventArgs());
            return trainingPointsToReturn;
        }


        private static void SearchMS2Spectrum(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, int ms2spectrumIndex, Peptide peptide, List<TrainingPoint> trainingPointsToReturn, int chargeStateFromMorpheus)
        {
            var countForThisMS2 = 0;
            var msDataScan = myMsDataFile.GetScan(ms2spectrumIndex);

            var productsB = peptide.Fragment(FragmentTypes.b, true).ToArray();
            var productsY = peptide.Fragment(FragmentTypes.y, true).ToArray();

            var rangeOfSpectrum = msDataScan.MzRange;

            foreach (ChemicalFormulaFragment fragment in productsB.Union(productsY))
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
                        var theTuple = Tuple.Create<double, double>(closestPeak.X, msDataScan.RetentionTime);
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
            p.OnWatch(new OutputHandlerEventArgs("  countForThisMS2  =   " + countForThisMS2));
        }

        private static void SearchMS1Spectra(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, IMzSpectrum<MzPeak> distributionSpectrum, List<TrainingPoint> trainingPointsToReturn, int ms2spectrumIndex, int direction, HashSet<Tuple<double, double>> peaksAddedHashSet)
        {
            var theIndex = -1;
            if (direction == 1)
                theIndex = ms2spectrumIndex;
            else
                theIndex = ms2spectrumIndex - 1;

            bool added = true;

            // Below should go in a loop!
            while (theIndex >= 0 && theIndex <= myMsDataFile.LastSpectrumNumber && added == true)
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

                var fullMS1spectrum = myMsDataFile.GetScan(theIndex);
                double ms1RetentionTime = fullMS1spectrum.RetentionTime;
                var rangeOfSpectrum = fullMS1spectrum.MzRange;
                var ms1FilteredByHighIntensities = fullMS1spectrum.MassSpectrum.FilterByIntensity(p.intensityCutoff, double.MaxValue);
                if (ms1FilteredByHighIntensities.Count == 0)
                {
                    theIndex += direction;
                    continue;
                }

                for (int chargeToLookAt = 1; ; chargeToLookAt++)
                {
                    IMzSpectrum<MzPeak> chargedDistribution = distributionSpectrum.CorrectMasses(s => (s + chargeToLookAt * Constants.Proton) / chargeToLookAt);

                    if ((p.MS2spectraToWatch.Contains(ms2spectrumIndex) || p.MS1spectraToWatch.Contains(theIndex)) && chargedDistribution.GetMzRange().IsOverlapping(p.mzRange))
                    {
                        p.OnWatch(new OutputHandlerEventArgs("  chargedDistribution: " + string.Join(", ", chargedDistribution.Take(4)) + "..."));
                    }

                    if (chargedDistribution.LastMZ > rangeOfSpectrum.Maximum)
                        continue;
                    if (chargedDistribution.GetBasePeak().MZ < rangeOfSpectrum.Minimum)
                        break;

                    List<TrainingPoint> trainingPointsToAverage = new List<TrainingPoint>();
                    for (int isotopologueIndex = 0; isotopologueIndex < Math.Min(p.numIsotopologuesToConsider, chargedDistribution.Count); isotopologueIndex++)
                    {
                        var closestPeak = ms1FilteredByHighIntensities.GetClosestPeak(chargedDistribution[isotopologueIndex].MZ);
                        var theTuple = Tuple.Create<double, double>(closestPeak.X, ms1RetentionTime);
                        if (Math.Abs(chargedDistribution[isotopologueIndex].MZ - closestPeak.X) < p.toleranceInMZforSearch && !peaksAddedHashSet.Contains(theTuple))
                        {
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

        private static List<IMzSpectrum<MzPeak>> CalibrateSpectra(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, CalibrationFunction cf)
        {
            List<IMzSpectrum<MzPeak>> calibratedSpectra = new List<IMzSpectrum<MzPeak>>();
            for (int i = 0; i < myMsDataFile.LastSpectrumNumber; i++)
            {
                if (i % (myMsDataFile.LastSpectrumNumber / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs((i / (myMsDataFile.LastSpectrumNumber / 100))));
                if (p.MS1spectraToWatch.Contains(i + 1))
                {
                    p.OnWatch(new OutputHandlerEventArgs("Before calibration of spectrum " + (i + 1)));
                    var mzs = myMsDataFile.GetSpectrum(i+1).Extract(p.mzRange);
                    p.OnWatch(new OutputHandlerEventArgs(string.Join(", ", mzs)));
                }
                calibratedSpectra.Add(myMsDataFile.GetSpectrum(i+1).CorrectMasses(s => s - cf.Predict(new DataPoint(s, myMsDataFile.GetScan(i+1).RetentionTime))));
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

        private static List<double> CalibratePrecursorMZs(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, CalibrationFunction cf)
        {
            List<double> calibratedPrecursorMZs = new List<double>();
            double precursorTime = -1;
            for (int i = 0; i < myMsDataFile.LastSpectrumNumber; i++)
            {
                if (i % (myMsDataFile.LastSpectrumNumber / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs((i / (myMsDataFile.LastSpectrumNumber / 100))));
                double newMZ = -1;
                if (myMsDataFile.GetScan(i+1).MsnOrder == 1)
                {
                    precursorTime = myMsDataFile.GetScan(i+1).RetentionTime;
                }
                else
                {
                    newMZ = myMsDataFile.GetScan(i+1).SelectedIonMonoisotopicMZ - cf.Predict(new DataPoint(myMsDataFile.GetScan(i+1).SelectedIonMonoisotopicMZ, precursorTime));
                }
                calibratedPrecursorMZs.Add(newMZ);
            }
            p.OnOutput(new OutputHandlerEventArgs());
            return calibratedPrecursorMZs;
        }

        private static string ConvertToCSMSLFormula(string theFormula)
        {
            theFormula = Regex.Replace(theFormula, @"[\s()]", "");
            return theFormula;
        }

        private static int GetLastNumberFromString(string s)
        {
            return Convert.ToInt32(Regex.Match(s, @"\d+$").Value);
        }

        private static indexedmzML CreateMyIndexedMZmlwithCalibratedSpectra(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, List<IMzSpectrum<MzPeak>> calibratedSpectra, List<double> calibratedPrecursorMZs)
        {
            indexedmzML _indexedmzMLConnection = new indexedmzML();
            _indexedmzMLConnection.mzML = new mzMLType();
            _indexedmzMLConnection.mzML.version = "1";

            _indexedmzMLConnection.mzML.cvList = new CVListType();
            _indexedmzMLConnection.mzML.cvList.count = "1";
            _indexedmzMLConnection.mzML.cvList.cv = new CVType[1];
            _indexedmzMLConnection.mzML.cvList.cv[0] = new CVType();
            _indexedmzMLConnection.mzML.cvList.cv[0].URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo";
            _indexedmzMLConnection.mzML.cvList.cv[0].fullName = "Proteomics Standards Initiative Mass Spectrometry Ontology";
            _indexedmzMLConnection.mzML.cvList.cv[0].id = "MS";

            _indexedmzMLConnection.mzML.fileDescription = new FileDescriptionType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent = new ParamGroupType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam = new CVParamType[2];
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0].accession = "MS:1000579"; // MS1 Data
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1].accession = "MS:1000580"; // MSn Data

            _indexedmzMLConnection.mzML.softwareList = new SoftwareListType();
            _indexedmzMLConnection.mzML.softwareList.count = "1";

            _indexedmzMLConnection.mzML.softwareList.software = new SoftwareType[1];
            // For a RAW file!!!
            // ToDo: read softwareList from mzML file
            //_indexedmzMLConnection.mzML.softwareList.software[1] = new SoftwareType();
            //_indexedmzMLConnection.mzML.softwareList.software[1].id = "ThermoSoftware";
            //_indexedmzMLConnection.mzML.softwareList.software[1].version = rawFile.GetSofwareVersion();
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam = new CVParamType[1];
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0] = new CVParamType();
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0].accession = "MS:1000693";

            _indexedmzMLConnection.mzML.softwareList.software[0] = new SoftwareType();
            _indexedmzMLConnection.mzML.softwareList.software[0].id = "StefanSoftware";
            _indexedmzMLConnection.mzML.softwareList.software[0].version = "1";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam = new CVParamType[1];
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].accession = "MS:1000799";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].value = "StefanSoftware";


            // Leaving empty. Can't figure out the configurations. 
            // ToDo: read instrumentConfigurationList from mzML file
            _indexedmzMLConnection.mzML.instrumentConfigurationList = new InstrumentConfigurationListType();

            _indexedmzMLConnection.mzML.dataProcessingList = new DataProcessingListType();
            // Only writing mine! Might have had some other data processing (but not if it is a raw file)
            // ToDo: read dataProcessingList from mzML file
            _indexedmzMLConnection.mzML.dataProcessingList.count = "1";
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing = new DataProcessingType[1];
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0] = new DataProcessingType();
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0].id = "StefanDataProcessing";


            _indexedmzMLConnection.mzML.run = new RunType();

            // ToDo: Finish the chromatogram writing!
            _indexedmzMLConnection.mzML.run.chromatogramList = new ChromatogramListType();
            _indexedmzMLConnection.mzML.run.chromatogramList.count = "1";
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram = new ChromatogramType[1];
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram[0] = new ChromatogramType();

            _indexedmzMLConnection.mzML.run.spectrumList = new SpectrumListType();
            _indexedmzMLConnection.mzML.run.spectrumList.count = (myMsDataFile.LastSpectrumNumber).ToString();
            _indexedmzMLConnection.mzML.run.spectrumList.defaultDataProcessingRef = "StefanDataProcessing";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new SpectrumType[myMsDataFile.LastSpectrumNumber];

            // Loop over all spectra
            for (int i = 0; i < myMsDataFile.LastSpectrumNumber; i++)
            {
                if (i % (myMsDataFile.LastSpectrumNumber / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs((i / (myMsDataFile.LastSpectrumNumber / 100))));
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i] = new SpectrumType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].defaultArrayLength = myMsDataFile.GetSpectrum(i+1).Count;
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].index = i.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].id = myMsDataFile.GetScan(i+1).id;

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam = new CVParamType[7];

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0] = new CVParamType();

                if (myMsDataFile.GetScan(i+1).MsnOrder == 1)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000579";
                }
                else if (myMsDataFile.GetScan(i+1).MsnOrder == 2)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000580";

                    // So needs a precursor!
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList = new PrecursorListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor = new PrecursorType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0] = new PrecursorType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].spectrumRef = myMsDataFile.GetScan(i+1).PrecursorID;
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList = new SelectedIonListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon = new ParamGroupType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0] = new ParamGroupType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam = new CVParamType[3];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].name = "selected ion m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].value = calibratedPrecursorMZs[i].ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].accession = "MS:1000744";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].name = "charge state";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].value = myMsDataFile.GetScan(i+1).SelectedIonChargeState.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].accession = "MS:1000041";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].name = "peak intensity";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].value = myMsDataFile.GetScan(i+1).SelectedIonIsolationIntensity.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].accession = "MS:1000042";



                }

                // OPTIONAL, but need for CSMSL reader. ms level
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].name = "ms level";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].accession = "MS:1000511";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].value = myMsDataFile.GetScan(i+1).MsnOrder.ToString();

                // Centroid?
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2] = new CVParamType();
                if (myMsDataFile.GetScan(i+1).isCentroid)
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000127";
                else
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000128";

                // Polarity
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3] = new CVParamType();
                if (myMsDataFile.GetScan(i+1).Polarity == Polarity.Negative)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "negative scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000129";
                }
                else if (myMsDataFile.GetScan(i+1).Polarity == Polarity.Positive)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "positive scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000130";
                }

                // Spectrum title
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].name = "spectrum title";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].accession = "MS:1000796";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].value = myMsDataFile.GetScan(i+1).id;


                // Lowest observed mz
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].name = "lowest observed m/z";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].accession = "MS:1000528";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].value = calibratedSpectra[i].FirstMZ.ToString();


                // Highest observed mz
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].name = "highest observed m/z";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].accession = "MS:1000527";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].value = calibratedSpectra[i].LastMZ.ToString();



                // Retention time
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList = new ScanListType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.count = "1";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan = new ScanType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0] = new ScanType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam = new CVParamType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].name = "scan start time";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].accession = "MS:1000016";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].value = myMsDataFile.GetScan(i+1).RetentionTime.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitCvRef = "UO";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitAccession = "UO:0000031";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitName = "minute";

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList = new BinaryDataArrayListType();

                // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
                // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.count = 2.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray = new BinaryDataArrayType[2];

                // M/Z Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].binary = Mzml.ConvertDoublestoBase64(calibratedSpectra[i].GetMasses(), false, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam = new CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000514";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "m/z array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].name = "64-bit float";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000574";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "zlib compression";

                // Intensity Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].binary = Mzml.ConvertDoublestoBase64(calibratedSpectra[i].GetIntensities(), false, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam = new CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000515";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "intensity array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].name = "64-bit float";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000574";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "zlib compression";
            }
            p.OnOutput(new OutputHandlerEventArgs());

            return _indexedmzMLConnection;
        }

    }


    public class AnEntry
    {
        public AnEntry(string spectraFile, string mzidFile)
        {
            this.spectraFile = spectraFile;
            this.mzidFile = mzidFile;
        }

        [DisplayName("Spectra File")]
        public string spectraFile { get; set; }
        [DisplayName("Mzid File")]
        public string mzidFile { get; set; }
    }

}