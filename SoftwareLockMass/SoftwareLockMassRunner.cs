using Spectra;
using System;
using System.Collections.Generic;
using System.IO;

namespace SoftwareLockMass
{
    public static class SoftwareLockMassRunner
    {
        public static void Run(SoftwareLockMassParams p)
        {
            p.OnOutput(new OutputHandlerEventArgs("Welcome to my software lock mass implementation"));
            p.OnOutput(new OutputHandlerEventArgs("Calibrating " + Path.GetFileName(p.myMsDataFile.FilePath)));

            p.myMsDataFile.Open();

            p.OnOutput(new OutputHandlerEventArgs("Getting Training Points"));
            List<TrainingPoint> trainingPoints = TrainingPointsExtractor.GetTrainingPoints(p.myMsDataFile, p.identifications, p);

            //p.OnOutput(new OutputHandlerEventArgs("Writing training points to file"));
            //WriteTrainingDataToFiles(trainingPoints);

            p.OnOutput(new OutputHandlerEventArgs("Train the calibration model"));
            CalibrationFunction cf;
            //CalibrationFunction cf = new IdentityCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new ConstantCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new LinearCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new CubicCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new QuarticCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new CalibrationFunctionClustering(p.OnOutput, 20);
            //CalibrationFunction cf = new MedianCalibrationFunction(p.OnOutput);
            //CalibrationFunction cf = new KDTreeCalibrationFunction(p.OnOutput);
            try
            {
                cf = new QuarticCalibrationFunction(p.OnOutput);
                cf.Train(trainingPoints);
            }
            catch (ArgumentException)
            {
                try
                {
                    cf = new CubicCalibrationFunction(p.OnOutput);
                    cf.Train(trainingPoints);
                }
                catch (ArgumentException)
                {
                    try
                    {
                        cf = new QuadraticCalibrationFunction(p.OnOutput);
                        cf.Train(trainingPoints);
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            cf = new LinearCalibrationFunction(p.OnOutput);
                            cf.Train(trainingPoints);
                        }
                        catch (ArgumentException)
                        {
                            cf = new ConstantCalibrationFunction(p.OnOutput);
                            cf.Train(trainingPoints);
                        }
                    }
                }
            }

            if (p.tsvFile != null)
            {
                p.OnOutput(new OutputHandlerEventArgs("Calibrating TSV file"));
                Calibrators.CalibrateTSV(cf, p);
            }
            p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
            List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(cf, p);
            p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
            List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(cf, p);

            p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs);

            p.OnOutput(new OutputHandlerEventArgs("Finished running my software lock mass implementation"));
        }

        private static void WriteTrainingDataToFiles(List<TrainingPoint> trainingPoints)
        {
            using (StreamWriter file = new StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData1.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.mz);
            using (StreamWriter file = new StreamWriter(@"E:\Stefan\data\CalibratedOutput\trainingData2.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.rt);
            using (StreamWriter file = new StreamWriter(@"E:\Stefan\data\CalibratedOutput\labelData.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.l);
        }

    }
}