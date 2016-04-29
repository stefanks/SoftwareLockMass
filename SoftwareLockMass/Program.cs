using CSMSL.IO.MzML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSMSL.IO.Thermo;
using CSMSL.IO;
using CSMSL.Spectral;
using System.IO;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace SoftwareLockMass
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to my software lock mass implementation");

            Console.WriteLine("Reading uncalibrated raw/mzML file");

            IMSDataFile<ISpectrum<IPeak>> myMSDataFile = new ThermoRawFile(@"E:\Stefan\data\120426_Jurkat_highLC_Frac1.raw");
            //IMSDataFile<ISpectrum<IPeak>> myMSDataFile = new ThermoRawFile(@"E:\Stefan\data\ToyData\Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW");
            //IMSDataFile<ISpectrum<IPeak>> myMSDataFile = new Mzml(@"E:\Stefan\data\ToyData\tiny.pwiz.1.1.mzML");

            myMSDataFile.Open();

            // Code to list spectrum indices of MS1 spectra
            //for (int i = 0; i < myMSDataFile.LastSpectrumNumber; i++)
            //{
            //    if (myMSDataFile[i + 1].MsnOrder == 1)
            //    {
            //        Console.WriteLine((i + 1));
            //    }
            //}

            //Console.WriteLine("Spectrum number 1, first peak:");
            //Console.WriteLine(myMSDataFile[1].MassSpectrum.GetPeak(0));
            Console.WriteLine("Spectrum number 17, first peak:");
            Console.WriteLine(myMSDataFile[17].MassSpectrum.GetPeak(0));
            Console.WriteLine(myMSDataFile[17].id);

            Console.WriteLine("Performing calibration");
            Console.WriteLine("Currently training points are ONLY the output from morpheus");
            Console.WriteLine("NOT using neighboring scans to train");
            string pepFileLocation = @"E:\Stefan\data\morpheusRawOutput\120426_Jurkat_highLC_Frac1.mzid";
            List<CalibratedSpectrum> calibratedSpectra = Calibrate(myMSDataFile, pepFileLocation);
            
            Console.WriteLine("Creating _indexedmzMLConnection, and putting data in it");

            indexedmzML _indexedmzMLConnection = GetMyIndexedMZml(myMSDataFile, calibratedSpectra);

            Console.WriteLine("Writing calibrated mzML file");
            
            string outputFilePath = @"E:\Stefan\data\calibratedOutput.mzML";

            Mzml.Write(outputFilePath, _indexedmzMLConnection);

            Console.WriteLine("Reading calibrated mzML file for verification");

            Mzml mzmlFile2 = new Mzml(outputFilePath);

            mzmlFile2.Open();

            Console.WriteLine("Number of spectra:{0}", mzmlFile2.LastSpectrumNumber);

            //Console.WriteLine("Spectrum number 1, first peak:");
            //Console.WriteLine(mzmlFile2[1].MassSpectrum[0]);
            Console.WriteLine("Spectrum number 17, first peak:");
            Console.WriteLine(mzmlFile2[17].MassSpectrum[0]);

            Console.WriteLine("Finished running my software lock mass implementation");
            Console.Read();
        }

        private static List<CalibratedSpectrum> Calibrate(IMSDataFile<ISpectrum<IPeak>> myMSDataFile, string pepFileLocation)
        {
            XmlSerializer _indexedSerializer = new XmlSerializer(typeof(mzIdentML.MzIdentMLType));
            Stream stream = new FileStream(pepFileLocation, FileMode.Open);
            // Read the XML file into the variable
            mzIdentML.MzIdentMLType dd = _indexedSerializer.Deserialize(stream) as mzIdentML.MzIdentMLType;

            // Get the training data out of xml
            List<double[]> trainingData = new List<double[]>();
            List<double> labelData = new List<double>();
            int matchIndex = 0;
            bool passThreshold;
            do
            {
                double[] trainingPoint = new double[2];

                double experiemntalMassToCharge = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].experimentalMassToCharge;
                double calculatedMassToCharge = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge;
                int chargeState = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].chargeState;
                string spectrumID = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].spectrumID;
                passThreshold = dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].passThreshold;
                int spectrumIndex = GetSpectrumIndexFromSpectrumID(spectrumID);
                double retentionTime = myMSDataFile[spectrumIndex].RetentionTime;
                trainingPoint[0] = calculatedMassToCharge;
                trainingPoint[1] = retentionTime;
                trainingData.Add(trainingPoint);
                labelData.Add((calculatedMassToCharge - experiemntalMassToCharge) * chargeState);
                matchIndex += 1;
            } while (passThreshold == true);

            // Create the calibration function

            CalibrationFunction cf = new CalibrationFunction(trainingData, labelData);

            List<CalibratedSpectrum> calibratedSpectra = new List<CalibratedSpectrum>();
            for (int i = 0; i < myMSDataFile.LastSpectrumNumber; i++)
            {
                var s = myMSDataFile[i+1];
                calibratedSpectra.Add(new CalibratedSpectrum());
                var mzValues = s.MassSpectrum.GetMasses();
                for (int j = 0; j < s.MassSpectrum.Count;j++)
                {
                    mzValues[j] += cf.calibrate(s.MassSpectrum.GetIntensities()[j], s.RetentionTime);
                }
                calibratedSpectra[i].AddMZValues(mzValues);
            }

            return calibratedSpectra;
        }

        private static int GetSpectrumIndexFromSpectrumID(string spectrumID)
        {
            return Convert.ToInt32(Regex.Match(spectrumID, @"\d+$").Value);
        }

        private static indexedmzML GetMyIndexedMZml(IMSDataFile<ISpectrum<IPeak>> myMSDataFile, List<CalibratedSpectrum> calibratedSpectra)
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
            _indexedmzMLConnection.mzML.run.spectrumList.count = myMSDataFile.LastSpectrumNumber.ToString();
            _indexedmzMLConnection.mzML.run.spectrumList.defaultDataProcessingRef = "StefanDataProcessing";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new SpectrumType[myMSDataFile.LastSpectrumNumber];
            
            // Loop over all spectra
            for (int i = 0; i < myMSDataFile.LastSpectrumNumber; i++)
            {
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i] = new SpectrumType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].defaultArrayLength = myMSDataFile[i + 1].MassSpectrum.Count;
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].index = i.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].id = myMSDataFile[i + 1].id;

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam = new CVParamType[3];

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
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList = new SelectedIonListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon = new ParamGroupType[1];
                    //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam = new CVParamType

                    // double a = rawFile.GetPrecursorMz(i + 1);

                }

                // OPTIONAL, but need for CSMSL reader
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].accession = "MS:1000511";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].value = myMSDataFile[i + 1].MsnOrder.ToString();

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2] = new CVParamType();
                if (myMSDataFile[i + 1].isCentroid)
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000127";
                else
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000128";


                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList = new BinaryDataArrayListType();

                // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
                // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.count = 2.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray = new BinaryDataArrayType[2];

                // M/Z Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].binary = Mzml.ConvertDoublestoBase64(calibratedSpectra[i].mzValues, true, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam = new CVParamType[3];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000574";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[2] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[2].accession = "MS:1000514";

                // Intensity Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].binary = Mzml.ConvertDoublestoBase64(myMSDataFile[i + 1].MassSpectrum.GetIntensities(), true, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam = new CVParamType[3];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000574";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[2] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[2].accession = "MS:1000515";
            }


            return _indexedmzMLConnection;
        }
    }
}
