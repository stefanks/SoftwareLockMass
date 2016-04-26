using CSMSL.IO.MzML;
using CSMSL.IO.Thermo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSMSL.Spectral;

namespace SoftwareLockMass
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to my software lock mass implementation");
            
            Console.WriteLine("Reading uncalibrated raw/mzML file");

            string thermoFileLocation = @"E:\Stefan\data\120426_Jurkat_highLC_Frac1.raw";
            ThermoRawFile rawFile = new ThermoRawFile(thermoFileLocation);
            rawFile.Open();
            rawFile.LoadAllScansInMemory();
            Console.WriteLine("Number of spectra:{0}", rawFile.LastSpectrumNumber);
            Console.WriteLine("Spectrum number 1:");
            Console.WriteLine(rawFile[1].ScanFilter);
            Console.WriteLine(rawFile[1].MassSpectrum[0]);
            var scan = rawFile[1];
            Console.WriteLine("{0,-4} {1,3} {2,-6:F4} {3,-5} {4,7} {5,-10} {6}", scan.SpectrumNumber, scan.MsnOrder, scan.RetentionTime, scan.Polarity, scan.MassSpectrum.Count, scan.MzAnalyzer, scan.MzRange);



            //Mzml mzmlFile = new Mzml(@"E:\Stefan\data\120426_Jurkat_highLC_Frac1_fromMSconvert.mzML");
            //mzmlFile.Open();
            //Console.WriteLine("Number of spectra:{0}", mzmlFile.LastSpectrumNumber);
            //Console.WriteLine("Spectrum number 1:");
            //Console.WriteLine(mzmlFile[1].ScanFilter);
            //Console.WriteLine(mzmlFile[1].MassSpectrum[0]);
            //var scan = mzmlFile[1];
            //Console.WriteLine("{0,-4} {1,3} {2,-6:F4} {3,-5} {4,7} {5,-10} {6}", scan.SpectrumNumber, scan.MsnOrder, scan.RetentionTime, scan.Polarity, scan.MassSpectrum.Count, scan.MzAnalyzer, scan.MzRange);

            //var rawFile = mzmlFile;

            Console.WriteLine("Creating _indexedmzMLConnection, and putting data in it");

            // Create single fields
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
            // Hack! In reality not sure that have (only) MS1 and MSn data!
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam = new CVParamType[2];
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0].accession = "MS:1000579";
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1].accession = "MS:1000580";

            _indexedmzMLConnection.mzML.softwareList = new SoftwareListType();
            // Assuming reading a raw file. mzML might have been pre-processed
            _indexedmzMLConnection.mzML.softwareList.count = 2.ToString();
            _indexedmzMLConnection.mzML.softwareList.software = new SoftwareType[2];
            _indexedmzMLConnection.mzML.softwareList.software[0] = new SoftwareType();
            _indexedmzMLConnection.mzML.softwareList.software[0].id = "ThermoSoftware";
            _indexedmzMLConnection.mzML.softwareList.software[0].version = rawFile.GetSofwareVersion();
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam = new CVParamType[1];
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].accession = "MS:1000693";

            _indexedmzMLConnection.mzML.softwareList.software[1] = new SoftwareType();
            _indexedmzMLConnection.mzML.softwareList.software[1].id = "StefanSoftware";
            _indexedmzMLConnection.mzML.softwareList.software[1].version = "1";
            _indexedmzMLConnection.mzML.softwareList.software[1].cvParam = new CVParamType[1];
            _indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0].accession = "MS:1000799";
            _indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0].value = "StefanSoftware";

            // Leaving empty. Can't figure out the configurations. 
            _indexedmzMLConnection.mzML.instrumentConfigurationList = new InstrumentConfigurationListType();
           
            _indexedmzMLConnection.mzML.dataProcessingList = new DataProcessingListType();
            // Only writing mine! Might have had some other data processing (but not if it is a raw file)
            _indexedmzMLConnection.mzML.dataProcessingList.count = 1.ToString();
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing = new DataProcessingType[1];
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0] = new DataProcessingType();
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0].id = "StefanDataProcessing";
            


            _indexedmzMLConnection.mzML.run = new RunType();
            
            _indexedmzMLConnection.mzML.run.chromatogramList = new ChromatogramListType();
            _indexedmzMLConnection.mzML.run.chromatogramList.count = 1.ToString();
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram = new ChromatogramType[1];
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram[0] = new ChromatogramType();

            _indexedmzMLConnection.mzML.run.spectrumList = new SpectrumListType();
            _indexedmzMLConnection.mzML.run.spectrumList.count = rawFile.LastSpectrumNumber.ToString();
            _indexedmzMLConnection.mzML.run.spectrumList.defaultDataProcessingRef = "StefanDataProcessing";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new SpectrumType[rawFile.LastSpectrumNumber];

            // Loop over all spectra
            for (int i = 0; i < rawFile.LastSpectrumNumber; i++)
            {
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i] = new SpectrumType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].defaultArrayLength = rawFile[i+1].MassSpectrum.Count;
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].index = i.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].id = rawFile.GetSpectrumID(i+1);

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam = new CVParamType[3];


                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0] = new CVParamType();
                if (rawFile[i + 1].MsnOrder == 1) { 
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000579";
                }
                else if (rawFile[i + 1].MsnOrder == 2) { 
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000580";

                    // So needs a precursor!
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList = new PrecursorListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor = new PrecursorType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0] = new PrecursorType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].



                    double a = rawFile.GetPrecursorMz(i + 1);


                }
                else
                    throw new Exception("Could not determine spectrum type");
                
                // OPTIONAL, but need for CSMSL reader
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].accession = "MS:1000511";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].value = rawFile[i + 1].MsnOrder.ToString();
                
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2] = new CVParamType();
                if (rawFile[i + 1].isCentroid)
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
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].binary = Mzml.ConvertDoublestoBase64(toMZdoubleArray(rawFile[i+1].MassSpectrum), true, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam = new CVParamType[3];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000574";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[2] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[2].accession = "MS:1000514";

                // Intensity Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].binary = Mzml.ConvertDoublestoBase64(toIntensitydoubleArray(rawFile[i + 1].MassSpectrum), true, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam = new CVParamType[3];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000574";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[2] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[2].accession = "MS:1000515";
           }



            // Console.WriteLine("Reading pep.xml file to determine calibration");
            // string pepFileLocation = @"E:\Stefan\data\morpheusRawOutput\120426_Jurkat_highLC_Frac1.pep.xml";

            // Console.WriteLine("Performing calibration");
            Console.WriteLine("Writing calibrated mzML file");


            string outputFilePath = @"E:\Stefan\data\calibratedOutput.mzML";

            Mzml.Write(outputFilePath, _indexedmzMLConnection);

            Console.WriteLine("Reading calibrated mzML file for verification");

            Mzml mzmlFile2 = new Mzml(outputFilePath);

            mzmlFile2.Open();

            Console.WriteLine("Number of spectra:{0}", mzmlFile2.LastSpectrumNumber);

            Console.WriteLine("Spectrum number 1:");
           
            Console.WriteLine(mzmlFile2[1].MassSpectrum[0]);
            

            Console.WriteLine("Finished running my software lock mass implementation");
            Console.Read();
        }

        private static double[] toIntensitydoubleArray(MZSpectrum massSpectrum)
        {
            return massSpectrum.GetIntensities();
        }

        private static double[] toMZdoubleArray(MZSpectrum massSpectrum)
        {
            return massSpectrum.GetMasses();
        }

        private static double[] toIntensitydoubleArray(ThermoSpectrum massSpectrum)
        {
            double[] returnArray = new double[massSpectrum.Count];
            for (int i = 0; i < massSpectrum.Count; i++)
            {
                returnArray[i] = massSpectrum[i].Intensity;
            }
            return returnArray;
        }

        private static double[] toMZdoubleArray(ThermoSpectrum massSpectrum)
        {
            double[] returnArray = new double[massSpectrum.Count];
            for (int i = 0; i < massSpectrum.Count; i++) {
                returnArray[i] = massSpectrum[i].MZ;
            }
            return returnArray;
        }
    }
}
