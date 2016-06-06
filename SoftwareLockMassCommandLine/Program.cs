using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using Spectra;
using System;
using System.Collections.Generic;
using UsefulProteomicsDatabases;

namespace SoftwareLockMass
{
    class Program
    {

        //private const string origDataFile = @"E:\Stefan\data\jurkat\MyUncalibrated.mzML";
        private const string origDataFile = @"E:\Stefan\data\jurkat\120426_Jurkat_highLC_Frac1.raw";
        //private const string mzidFile = @"E:\Stefan\data\morpheusmzMLoutput1\MyUncalibrated.mzid";
        private const string mzidFile = @"E:\Stefan\data\4FileExperiments\4FileExperiment10ppmForCalibration\120426_Jurkat_highLC_Frac1.mzid";
        private const string outputFilePath = @"E:\Stefan\data\CalibratedOutput\calibratedOutput1.mzML";

        public static string unimodLocation = @"E:\Stefan\data\Unimod\unimod_tables.xml";
        public static string psimodLocation = @"E:\Stefan\data\PSI-MOD\PSI-MOD.obo.xml";
        public static string elementsLocation = @"E:\Stefan\data\Elements\elements.dat";

        static void Main(string[] args)
        {
            Loaders.unimodLocation = unimodLocation;
            Loaders.psimodLocation = psimodLocation;
            Loaders.elementLocation = elementsLocation;
            Loaders.LoadElements();

            List<AnEntry> theList = new List<AnEntry>();
            theList.Add(new AnEntry(origDataFile, mzidFile));

            SoftwareLockMassRunner.p = new SoftwareLockMassParams(theList);
            SoftwareLockMassRunner.p.outputHandler += P_outputHandler;
            SoftwareLockMassRunner.p.progressHandler += P_progressHandler;
            //SoftwareLockMassRunner.p.watchHandler += P_outputHandler;

            SoftwareLockMassRunner.Run();

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