﻿using System;

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
            //Console.Read();
            origDataFile = args[0];
            mzidFile = args[1];
            double intensityCutoff = 1e3;
            double toleranceInMZforSearch = 1e-2;
            if (args.Length > 2)
                intensityCutoff = Convert.ToDouble(args[2]);
            if (args.Length > 3)
                toleranceInMZforSearch = Convert.ToDouble(args[3]);

            SoftwareLockMassIO.IO.Load();

            SoftwareLockMassParams a = SoftwareLockMassIO.IO.GetReady(origDataFile, P_outputHandler, P_progressHandler, P_watchHandler, mzidFile);

            SoftwareLockMassRunner.Run(a);
            Console.Read();
        }

        private static void P_progressHandler(object sender, ProgressHandlerEventArgs e)
        {
            Console.Write(e.progress + "% ");
        }

        private static void P_outputHandler(object sender, OutputHandlerEventArgs e)
        {
            Console.WriteLine(e.output);
        }

        private static void P_watchHandler(object sender, OutputHandlerEventArgs e)
        {
            Console.WriteLine(e.output);
        }
    }
}