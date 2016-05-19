namespace SoftwareLockMass
{
    class Program
    {

        //private const string origDataFile = @"E:\Stefan\data\jurkat\MyUncalibrated.mzML";
        private const string origDataFile = @"E:\Stefan\data\jurkat\120426_Jurkat_highLC_Frac1.raw";
        //private const string mzidFile = @"E:\Stefan\data\morpheusmzMLoutput1\MyUncalibrated.mzid";
        private const string mzidFile = @"E:\Stefan\data\4FileExperiments\4FileExperiment10ppmForCalibration\120426_Jurkat_highLC_Frac1.mzid";
        private const string outputFilePath = @"E:\Stefan\data\CalibratedOutput\calibratedOutput1.mzML";

        static void Main(string[] args)
        {
            SoftwareLockMassParams p = new SoftwareLockMassParams(origDataFile, mzidFile);
            p.outputFile = outputFilePath;
            SoftwareLockMassRunner asdfasdf = new SoftwareLockMassRunner(p);

            asdfasdf.Run();

        }
    }
}