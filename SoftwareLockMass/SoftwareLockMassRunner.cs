using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            var rnd = new Random();
            var shuffledTrainingPoints = trainingPoints.OrderBy(item => rnd.Next());

            //var trainList = shuffledTrainingPoints.Take(trainingPoints.Count * 3 / 4);
            var testList = shuffledTrainingPoints.Skip(trainingPoints.Count * 3 / 4);

            p.OnOutput(new OutputHandlerEventArgs("Train the calibration model"));
            CalibrationFunction cf = new IdentityCalibrationFunction(p.OnOutput);
            p.OnOutput(new OutputHandlerEventArgs("MSE: " + cf.getMSE(testList)));
            try
            {
                cf = new ConstantCalibrationFunction(p.OnOutput, trainingPoints);
                p.OnOutput(new OutputHandlerEventArgs("MSE: " + cf.getMSE(testList)));
                cf = new LinearCalibrationFunction(p.OnOutput, trainingPoints);
                p.OnOutput(new OutputHandlerEventArgs("MSE: " + cf.getMSE(testList)));
                cf = new QuadraticCalibrationFunction(p.OnOutput, trainingPoints);
                p.OnOutput(new OutputHandlerEventArgs("MSE: " + cf.getMSE(testList)));
                cf = new CubicCalibrationFunction(p.OnOutput, trainingPoints);
                p.OnOutput(new OutputHandlerEventArgs("MSE: " + cf.getMSE(testList)));
                cf = new QuarticCalibrationFunction(p.OnOutput, trainingPoints);
                p.OnOutput(new OutputHandlerEventArgs("MSE: " + cf.getMSE(testList)));
            }
            catch (ArgumentException)
            {

            }

            if (p.tsvFile != null)
            {
                p.OnOutput(new OutputHandlerEventArgs("Calibrating TSV file"));
                Calibrators.CalibrateTSV(cf, p);
            }
            p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
            List<IMzSpectrum<MzPeak, MzRange>> calibratedSpectra = Calibrators.CalibrateSpectra(cf, p);
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