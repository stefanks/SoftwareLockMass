using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using UsefulProteomicsDatabases;

namespace SoftwareLockMass
{
    class Program
    {

        //private const string origDataFile = @"E:\Stefan\data\jurkat\MyUncalibrated.mzML";
        //private const string origDataFile = @"E:\Stefan\data\jurkat\120426_Jurkat_highLC_Frac1.raw";
        private const string origDataFile = @"";
        //private const string mzidFile = @"E:\Stefan\data\morpheusmzMLoutput1\MyUncalibrated.mzid";
        //private const string mzidFile = @"E:\Stefan\data\4FileExperiments\4FileExperiment10ppmForCalibration\120426_Jurkat_highLC_Frac1.mzid";
        //private const string outputFilePath = @"E:\Stefan\data\CalibratedOutput\calibratedOutput1.mzML";
        private const string mzidFile = @"";
        private const string outputFilePath = @"";

        public static string unimodLocation = @"C:\Users\stepa\Data\Databases\Elements\unimod_tables.xml";
        public static string psimodLocation = @"C:\Users\stepa\Data\Databases\PSI-MOD\PSI-MOD.obo.xml";
        public static string elementsLocation = @"C:\Users\stepa\Data\Databases\Elements\elements.dat";

        static void Main(string[] args)
        {
            Loaders.unimodLocation = unimodLocation;
            Loaders.psimodLocation = psimodLocation;
            Loaders.elementLocation = elementsLocation;
            Loaders.LoadElements();

            IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile;
            if (Path.GetExtension(origDataFile).Equals(".mzML"))
            {
                myMsDataFile = new Mzml(origDataFile);
            }
            else
            {
                myMsDataFile = new ThermoRawFile(origDataFile);
            }

            //SoftwareLockMassRunner.p = new SoftwareLockMassParams(myMsDataFile);
            //SoftwareLockMassRunner.p.outputHandler += P_outputHandler;
            //SoftwareLockMassRunner.p.progressHandler += P_progressHandler;
            ////SoftwareLockMassRunner.p.watchHandler += P_outputHandler;

            //SoftwareLockMassRunner.Run();

        }

        private static void P_progressHandler(object sender, ProgressHandlerEventArgs e)
        {
            Console.Write(e.progress + "% ");
        }

        private static void P_outputHandler(object sender, OutputHandlerEventArgs e)
        {
            Console.WriteLine(e.output);
        }
    }
}