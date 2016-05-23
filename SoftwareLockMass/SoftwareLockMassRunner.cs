using Chemistry;
using CSMSL.IO.MzML;
using CSMSL.IO.Thermo;
using MassSpectrometry;
using MathNet.Numerics.Statistics;
using Proteomics;
using Spectra;
using System;
using System.Collections.Generic;
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
            Console.WriteLine("Welcome to my software lock mass implementation");
            IMSDataFile<ISpectrum<IPeak>> myMSDataFile = null;

            if (p.mzML())
            {
                Console.WriteLine("Reading uncalibrated mzML file");
                myMSDataFile = new Mzml(p.fileToCalibrate);
            }
            else{
                Console.WriteLine("Reading uncalibrated raw file");
                myMSDataFile = new ThermoRawFile(p.fileToCalibrate);
            }


            myMSDataFile.Open();

            //Console.WriteLine("Spectrum number 2810, peak num 2213: " + myMSDataFile[2810].MassSpectrum.GetPeak(2212).X);
            //Console.WriteLine("Spectrum number 2810, peak num 2394: " + myMSDataFile[2810].MassSpectrum.GetPeak(2393).X);
            //Console.WriteLine("Spectrum number 2813, SelectedIonMonoisotopicMZ: " + myMSDataFile[2813].SelectedIonMonoisotopicMZ);
            //Console.WriteLine("Spectrum number 11279, SelectedIonMonoisotopicMZ: " + myMSDataFile[11279].SelectedIonMonoisotopicMZ);

            Console.WriteLine("Getting Training Points");
            List<TrainingPoint> trainingPoints = GetTrainingPoints(myMSDataFile, p.mzidFile);

            Console.WriteLine("Writing training points to file");
            WriteTrainingDataToFiles(trainingPoints);

            Console.WriteLine("Train the calibration model");
            //CalibrationFunction cf = new IdentityCalibrationFunction();
            //CalibrationFunction cf = new ConstantCalibrationFunction();
            //CalibrationFunction cf = new LinearCalibrationFunction();
            //CalibrationFunction cf = new QuadraticCalibrationFunction();
            //CalibrationFunction cf = new CubicCalibrationFunction();
            CalibrationFunction cf = new QuarticCalibrationFunction();
            //CalibrationFunction cf = new CalibrationFunctionClustering(20);
            //CalibrationFunction cf = new MedianCalibrationFunction();
            //CalibrationFunction cf = new KDTreeCalibrationFunction();
            cf.Train(trainingPoints);

            Console.WriteLine("Computing Mean Squared Error ");
            Console.WriteLine("The Mean Squared Error for the model is " + cf.getMSE(trainingPoints));

            Console.WriteLine("Calibrating Spectra");
            List<ISpectrum> calibratedSpectra = CalibrateSpectra(myMSDataFile, cf);
            Console.WriteLine("Calibrating Precursor MZs");
            List<double> calibratedPrecursorMZs = CalibratePrecursorMZs(myMSDataFile, cf);

            Console.WriteLine("Creating _indexedmzMLConnection, and putting data in it");
            indexedmzML _indexedmzMLConnection = CreateMyIndexedMZmlwithCalibratedSpectra(myMSDataFile, calibratedSpectra, calibratedPrecursorMZs);

            Console.WriteLine("Writing calibrated mzML file");
            Mzml.Write(p.outputFile, _indexedmzMLConnection);

            //Console.WriteLine("Reading calibrated mzML file for verification");
            //Mzml mzmlFile2 = new Mzml(outputFilePath);
            //mzmlFile2.Open();

            //Console.WriteLine("Spectrum number 2810, peak num 2213: " + mzmlFile2[2810].MassSpectrum.GetPeak(2212).MZ);
            //Console.WriteLine("Spectrum number 2810, peak num 2394: " + mzmlFile2[2810].MassSpectrum.GetPeak(2393).MZ);
            //Console.WriteLine("Spectrum number 2813, SelectedIonMonoisotopicMZ: " + mzmlFile2[2813].SelectedIonMonoisotopicMZ);
            //Console.WriteLine("Spectrum number 11279, SelectedIonMonoisotopicMZ: " + mzmlFile2[11279].SelectedIonMonoisotopicMZ);

            Console.WriteLine("Finished running my software lock mass implementation");
            Console.Read();
        }

        private static void WriteTrainingDataToFiles(List<TrainingPoint> trainingPoints)
        {
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData1.dat"))
            {
                foreach (TrainingPoint d in trainingPoints)
                {
                    file.WriteLine(d.dp.mz);
                }
            }

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData2.dat"))
            {
                foreach (TrainingPoint d in trainingPoints)
                {
                    file.WriteLine(d.dp.rt);
                }
            }

            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"E:\Stefan\data\CalibratedOutput\labelData.dat"))
            {
                foreach (TrainingPoint d in trainingPoints)
                {
                    file.WriteLine(d.l);
                }
            }
        }

        private static List<TrainingPoint> GetTrainingPoints(IMSDataFile<ISpectrum<IPeak>> myMSDataFile, string mzidFile)
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

            // Loop over all results from the mzIdentML file
            for (int matchIndex = 0; matchIndex < dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult.Length; matchIndex++)
            {
                //Console.WriteLine(matchIndex);
                if (matchIndex % (dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult.Length / 100) == 0)
                    Console.Write("" + (matchIndex / (dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult.Length / 100)) + "% ");
                if (dd.SequenceCollection.PeptideEvidence[matchIndex].isDecoy)
                    continue;
                if (Convert.ToDouble(dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].cvParam[1].value) > p.thresholdPassParameter)
                    break;

                string ms2spectrumID = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].spectrumID;

                int ms2spectrumIndex = GetLastNumberFromString(ms2spectrumID);
                //if (ms2spectrumIndex == 2813 || ms2spectrumIndex == 11279 || ms2spectrumIndex == 11357 || ms2spectrumIndex == 4903 || ms2spectrumIndex == 3181)
                //if (ms2spectrumIndex == 13047 || ms2spectrumIndex == 13060 || ms2spectrumIndex == 14992)
                //{
                //    Console.WriteLine(" ms2spectrumIndex: " + ms2spectrumIndex);
                //    Console.WriteLine(" calculatedMassToCharge: " + dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge);
                //    Console.WriteLine(" experimentalMassToCharge: " + dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].experimentalMassToCharge);
                //    Console.WriteLine(" Error according to single morpheus point: " + ((dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].experimentalMassToCharge) - (dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge)));
                //}
                Spectrum<MZPeak> distributionSpectrum;
                if (p.MZID_MASS_DATA)
                {
                    double calculatedMassToCharge = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge;
                    int chargeState = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].chargeState;
                    distributionSpectrum = new MZSpectrum(new double[1] { calculatedMassToCharge * chargeState - chargeState * Constants.Proton }, new double[1] { 1 });
                }
                else
                {
                    // GET MASS DATA FROM PEPTIDE!  
                    //if (ms2spectrumIndex == 2813 || ms2spectrumIndex == 11279 || ms2spectrumIndex == 11357 || ms2spectrumIndex == 4903 || ms2spectrumIndex == 3181)
                    //if (ms2spectrumIndex == 13047 || ms2spectrumIndex == 13060 || ms2spectrumIndex == 14992)
                    //{
                    //    Console.WriteLine(" " + dd.SequenceCollection.Peptide[matchIndex].PeptideSequence);
                    //}
                    Peptide peptide1 = new Peptide(dd.SequenceCollection.Peptide[matchIndex].PeptideSequence);
                    if (dd.SequenceCollection.Peptide[matchIndex].Modification != null)
                    {
                        for (int i = 0; i < dd.SequenceCollection.Peptide[matchIndex].Modification.Length; i++)
                        {
                            int location = dd.SequenceCollection.Peptide[matchIndex].Modification[i].location;
                            string theFormula = null;
                            if (dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef == "UNIMOD")
                            {
                                //Console.WriteLine("UNIMOD modification");
                                string unimodAcession = dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].accession;
                                var indexToLookFor = GetLastNumberFromString(unimodAcession) - 1;
                                while (unimodDeserialized.modifications[indexToLookFor].record_id != GetLastNumberFromString(unimodAcession))
                                    indexToLookFor--;
                                theFormula = unimodDeserialized.modifications[indexToLookFor].composition;
                            }
                            else if (dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef == "PSI-MOD")
                            {
                                //Console.WriteLine("PSI-MOD modification");
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
                                            //Console.WriteLine(theFormula);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                                throw new Exception("Not familiar with modification dictionary " + dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef);
                            //Console.WriteLine(theFormula);
                            ChemicalFormulaModification modification = new ChemicalFormulaModification(ConvertToCSMSLFormula(theFormula));
                            //Console.WriteLine(modification.MonoisotopicMass);
                            peptide1.AddModification(modification, location);
                        }
                    }
                    // Calculate isotopic distribution
                    IsotopicDistribution dist = new IsotopicDistribution(p.fineResolution);
                    double[] masses;
                    double[] intensities;
                    dist.CalculateDistribuition(peptide1.GetChemicalFormula(), out masses, out intensities);
                    var fullSpectrum = new MZSpectrum(masses, intensities, false);
                    distributionSpectrum = fullSpectrum.FilterByNumberOfMostIntense(Math.Min(p.numIsotopologuesToConsider, fullSpectrum.Count));
                }

                SearchMS1Spectra(myMSDataFile, distributionSpectrum, trainingPointsToReturn, ms2spectrumIndex, -1, peaksAddedHashSet);
                SearchMS1Spectra(myMSDataFile, distributionSpectrum, trainingPointsToReturn, ms2spectrumIndex, 1, peaksAddedHashSet);



            }
            Console.WriteLine();
            return trainingPointsToReturn;
        }

        private static void SearchMS1Spectra(IMSDataFile<ISpectrum<IPeak>> myMSDataFile, Spectrum<MZPeak> distributionSpectrum, List<TrainingPoint> trainingPointsToReturn, int ms2spectrumIndex, int direction, HashSet<Tuple<double, double>> peaksAddedHashSet)
        {
            var theIndex = -1;
            if (direction == 1)
                theIndex = ms2spectrumIndex;
            else
                theIndex = ms2spectrumIndex - 1;

            bool added = true;

            // Below should go in a loop!
            while (theIndex >= 0 && theIndex <= myMSDataFile.LastSpectrumNumber && added == true)
            {
                if (myMSDataFile[theIndex].MsnOrder > 1)
                {
                    theIndex += direction;
                    continue;
                }
                added = false;
                //if (ms2spectrumIndex == 13047 || ms2spectrumIndex == 13060 || ms2spectrumIndex == 14992)
                //if (theIndex == 5984)
                //{
                //    Console.WriteLine(" Looking in MS1 spectrum " + theIndex);
                //}

                var fullMS1spectrum = myMSDataFile[theIndex];
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
                    Spectrum<MZPeak> chargedDistribution = distributionSpectrum.CorrectMasses(s => (s + chargeToLookAt * Constants.Proton) / chargeToLookAt);

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
                            //if (ms2spectrumIndex == 2813 || ms2spectrumIndex == 11279 || ms2spectrumIndex == 11357 || ms2spectrumIndex == 4903 || ms2spectrumIndex == 3181)
                            //if (theIndex == 5984)
                            //{
                            //    Console.WriteLine("   Looking for " + chargedDistribution[isotopologueIndex].MZ);
                            //    Console.WriteLine("   Found       " + closestPeak.X);
                            //    Console.WriteLine("   Error is    " + (closestPeak.X - chargedDistribution[isotopologueIndex].MZ));
                            //}
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

                        //    if (ms2spectrumIndex == 2813 || ms2spectrumIndex == 11279 || ms2spectrumIndex == 11357 || ms2spectrumIndex == 4903 || ms2spectrumIndex == 3181)
                        //if (ms2spectrumIndex == 13047 || ms2spectrumIndex == 13060 || ms2spectrumIndex == 14992)
                        //if (theIndex == 5984 && a.dp.mz > 587 && a.dp.mz < 588)
                        //{
                        //    Console.WriteLine();
                        //    Console.WriteLine("  Adding aggregate of " + trainingPointsToAverage.Count + " points");
                        //    Console.WriteLine("  a.dp.mz " + a.dp.mz);
                        //    Console.WriteLine("  a.dp.rt " + a.dp.rt);
                        //    Console.WriteLine("  a.l     " + a.l);
                        //    Console.WriteLine("chargedDistribution: " + string.Join(", ", chargedDistribution));
                        //    Console.WriteLine("ms2spectrumIndex: " + ms2spectrumIndex);
                        //}
                        trainingPointsToReturn.Add(a);
                    }
                }
                theIndex += direction;
            }
        }

        private static List<ISpectrum> CalibrateSpectra(IMSDataFile<ISpectrum<IPeak>> myMSDataFile, CalibrationFunction cf)
        {
            List<ISpectrum> calibratedSpectra = new List<ISpectrum>();
            for (int i = 0; i < myMSDataFile.LastSpectrumNumber; i++)
            {
                if (i % (myMSDataFile.LastSpectrumNumber / 100) == 0)
                    Console.Write("" + (i / (myMSDataFile.LastSpectrumNumber / 100)) + "% ");
                calibratedSpectra.Add(myMSDataFile[i + 1].MassSpectrum.CorrectMasses(s => s - cf.Predict(new DataPoint(s, myMSDataFile[i + 1].RetentionTime))));
            }
            Console.WriteLine();
            return calibratedSpectra;
        }

        private static List<double> CalibratePrecursorMZs(IMSDataFile<ISpectrum<IPeak>> myMSDataFile, CalibrationFunction cf)
        {
            List<double> calibratedPrecursorMZs = new List<double>();
            double precursorTime = -1;
            for (int i = 0; i < myMSDataFile.LastSpectrumNumber; i++)
            {
                if (i % (myMSDataFile.LastSpectrumNumber / 100) == 0)
                    Console.Write("" + (i / (myMSDataFile.LastSpectrumNumber / 100)) + "% ");
                double newMZ = -1;
                if (myMSDataFile[i + 1].MsnOrder == 1)
                {
                    precursorTime = myMSDataFile[i + 1].RetentionTime;
                }
                else
                {
                    newMZ = myMSDataFile[i + 1].SelectedIonMonoisotopicMZ - cf.Predict(new DataPoint(myMSDataFile[i + 1].SelectedIonMonoisotopicMZ, precursorTime));
                }
                calibratedPrecursorMZs.Add(newMZ);
            }
            Console.WriteLine();
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

        private static indexedmzML CreateMyIndexedMZmlwithCalibratedSpectra(IMSDataFile<ISpectrum<IPeak>> myMSDataFile, List<ISpectrum> calibratedSpectra, List<double> calibratedPrecursorMZs)
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
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram = new CSMSL.IO.MzML.ChromatogramType[1];
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram[0] = new CSMSL.IO.MzML.ChromatogramType();

            _indexedmzMLConnection.mzML.run.spectrumList = new SpectrumListType();
            _indexedmzMLConnection.mzML.run.spectrumList.count = (myMSDataFile.LastSpectrumNumber).ToString();
            _indexedmzMLConnection.mzML.run.spectrumList.defaultDataProcessingRef = "StefanDataProcessing";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new SpectrumType[myMSDataFile.LastSpectrumNumber];

            // Loop over all spectra
            for (int i = 0; i < myMSDataFile.LastSpectrumNumber; i++)
            {
                if (i % (myMSDataFile.LastSpectrumNumber / 100) == 0)
                    Console.Write("" + (i / (myMSDataFile.LastSpectrumNumber / 100)) + "% ");
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i] = new SpectrumType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].defaultArrayLength = myMSDataFile[i + 1].MassSpectrum.Count;
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].index = i.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].id = myMSDataFile[i + 1].id;

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam = new CVParamType[7];

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0] = new CVParamType();

                if (myMSDataFile[i + 1].MsnOrder == 1)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000579";
                }
                else if (myMSDataFile[i + 1].MsnOrder == 2)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000580";

                    // So needs a precursor!
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList = new PrecursorListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor = new PrecursorType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0] = new PrecursorType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].spectrumRef = myMSDataFile[i + 1].PrecursorID;
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
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].value = myMSDataFile[i + 1].SelectedIonChargeState.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].accession = "MS:1000041";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].name = "peak intensity";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].value = myMSDataFile[i + 1].SelectedIonIsolationIntensity.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].accession = "MS:1000042";



                }

                // OPTIONAL, but need for CSMSL reader. ms level
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].name = "ms level";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].accession = "MS:1000511";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].value = myMSDataFile[i + 1].MsnOrder.ToString();

                // Centroid?
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2] = new CVParamType();
                if (myMSDataFile[i + 1].isCentroid)
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000127";
                else
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000128";

                // Polarity
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3] = new CVParamType();
                if (myMSDataFile[i + 1].Polarity == Polarity.Negative)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "negative scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000129";
                }
                else if (myMSDataFile[i + 1].Polarity == Polarity.Positive)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "positive scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000130";
                }

                // Spectrum title
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].name = "spectrum title";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].accession = "MS:1000796";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].value = myMSDataFile[i + 1].id;


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
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].value = myMSDataFile[i + 1].RetentionTime.ToString();
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
            Console.WriteLine();

            return _indexedmzMLConnection;
        }

    }
}