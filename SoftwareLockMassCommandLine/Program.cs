using System;

namespace SoftwareLockMass
{
    class Program
    {
        static void Main(string[] args)
        {
            string origDataFile = args[0];
            string mzidFile = args[1];

            SoftwareLockMassIO.IO.Load();

            bool deconvolute = true;
            SoftwareLockMassParams a = SoftwareLockMassIO.IO.GetReady(origDataFile, P_outputHandler, P_progressHandler, P_watchHandler, mzidFile, deconvolute);

            SoftwareLockMassRunner.Run(a);
            //Console.Read();
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