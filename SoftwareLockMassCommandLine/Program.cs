using System;

namespace SoftwareLockMass
{
    class Program
    {
        private static string origDataFile;
        private static string mzidFile;

        public static string unimodLocation = @"unimod_tables.xml";
        public static string psimodLocation = @"PSI-MOD.obo.xml";
        public static string elementsLocation = @"elements.dat";
        public static string uniprotLocation = @"ptmlist.txt";

        static void Main(string[] args)
        {
            origDataFile = args[0];
            mzidFile = args[1];

            SoftwareLockMassIO.IO.Load();

            SoftwareLockMassParams a = SoftwareLockMassIO.IO.GetReady(origDataFile, P_outputHandler, P_progressHandler, mzidFile);

            SoftwareLockMassRunner.Run(a);

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