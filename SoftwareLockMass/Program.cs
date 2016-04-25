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

            Console.WriteLine("Converting data to _indexedmzMLConnection");

            indexedmzML _indexedmzMLConnection = new indexedmzML();
            _indexedmzMLConnection.mzML = new mzMLType();
            _indexedmzMLConnection.mzML.run = new RunType();
            _indexedmzMLConnection.mzML.run.spectrumList = new SpectrumListType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new SpectrumType[1];
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0] = new SpectrumType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList = new BinaryDataArrayListType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray = new BinaryDataArrayType[2];
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0] = new BinaryDataArrayType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1] = new BinaryDataArrayType();
            _indexedmzMLConnection.mzML.run.spectrumList.count = "1";

            double[] toConvert = toMZdoubleArray(rawFile[1].MassSpectrum);

            Console.WriteLine("The array starts with: {0}", toConvert[0]);

            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].binary = Mzml.ConvertDoublestoBase64(toConvert, true, false);

            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].cvParam = new CVParamType[3];
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000574";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new CVParamType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].cvParam[2] = new CVParamType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[0].cvParam[2].accession = "MS:1000514";

            toConvert = toIntensitydoubleArray(rawFile[1].MassSpectrum);

            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].binary = Mzml.ConvertDoublestoBase64(toConvert, true, false);

            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].cvParam = new CVParamType[3];
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000574";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new CVParamType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].cvParam[2] = new CVParamType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].binaryDataArrayList.binaryDataArray[1].cvParam[2].accession = "MS:1000515";

        


            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].cvParam = new CVParamType[1];
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].cvParam[0].accession = "MS:1000511";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum[0].cvParam[0].value = "1";


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
