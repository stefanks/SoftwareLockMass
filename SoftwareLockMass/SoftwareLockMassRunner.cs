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

            p.OnOutput(new OutputHandlerEventArgs("Opening file:"));
            p.myMsDataFile.Open();

            p.OnOutput(new OutputHandlerEventArgs("Getting Training Points"));
            List<TrainingPoint> trainingPoints = TrainingPointsExtractor.GetTrainingPoints(p.myMsDataFile, p.identifications, p);

            p.OnOutput(new OutputHandlerEventArgs("Writing training points to file"));
            WriteDataToFiles(trainingPoints, "all");

            var rnd = new Random();
            var shuffledTrainingPoints = trainingPoints.OrderBy(item => rnd.Next()).ToArray();

            var trainList = shuffledTrainingPoints.Take(trainingPoints.Count * 3 / 4).ToArray();
            var testList = shuffledTrainingPoints.Skip(trainingPoints.Count * 3 / 4).ToArray();

            var trainList1 = trainList.Where((b) => b.dp.msnOrder == 1).ToArray();
            WriteDataToFiles(trainList1, "train1");
            var trainList2 = trainList.Where((b) => b.dp.msnOrder == 2).ToArray();
            WriteDataToFiles(trainList2, "train2");

            CalibrationFunction bestCf = new IdentityCalibrationFunction(p.OnOutput);
            double bestMSE = bestCf.getMSE(testList);
            p.OnOutput(new OutputHandlerEventArgs("Original uncalibrated MSE: " + bestMSE));

            p.OnOutput(new OutputHandlerEventArgs("Train the calibration model"));
            try
            {
                CalibrationFunction cf = new ConstantCalibrationFunction(p.OnOutput, trainList);
                double mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Constant: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SeparateCalibrationFunction(new ConstantCalibrationFunction(p.OnOutput, trainList1), new ConstantCalibrationFunction(p.OnOutput, trainList2));

                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Constant separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new LinearCalibrationFunction(p.OnOutput, trainList);
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Linear: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SeparateCalibrationFunction(new LinearCalibrationFunction(p.OnOutput, trainList1), new LinearCalibrationFunction(p.OnOutput, trainList2));

                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Linear separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new QuadraticCalibrationFunction(p.OnOutput, trainList);
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Quadratic: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SeparateCalibrationFunction(new QuadraticCalibrationFunction(p.OnOutput, trainList1), new QuadraticCalibrationFunction(p.OnOutput, trainList2));

                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Quadratic separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }


                cf = new CubicCalibrationFunction(p.OnOutput, trainList);
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Cubic: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }


                cf = new SeparateCalibrationFunction(new CubicCalibrationFunction(p.OnOutput, trainList1), new CubicCalibrationFunction(p.OnOutput, trainList2));

                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Cubic separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }


                cf = new QuarticCalibrationFunction(p.OnOutput, trainList);
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Quartic: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SeparateCalibrationFunction(new QuarticCalibrationFunction(p.OnOutput, trainList1), new QuarticCalibrationFunction(p.OnOutput, trainList2));

                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Quartic separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }


                cf = new FifthCalibrationFunction(p.OnOutput, trainList);
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Fifth: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SeparateCalibrationFunction(new FifthCalibrationFunction(p.OnOutput, trainList1), new FifthCalibrationFunction(p.OnOutput, trainList2));

                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Fifth separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SixthCalibrationFunction(p.OnOutput, trainList);
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Sixth: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SeparateCalibrationFunction(new SixthCalibrationFunction(p.OnOutput, trainList1), new SixthCalibrationFunction(p.OnOutput, trainList2));

                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE Sixth separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new KDTreeCalibrationFunction(p.OnOutput, trainList);
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE KD Tree: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

                cf = new SeparateCalibrationFunction(new KDTreeCalibrationFunction(p.OnOutput, trainList1), new KDTreeCalibrationFunction(p.OnOutput, trainList2));
                mse = cf.getMSE(testList);
                p.OnOutput(new OutputHandlerEventArgs("MSE KD Tree separate: " + mse));
                if (mse < bestMSE)
                {
                    bestMSE = mse;
                    bestCf = cf;
                }

            }
            catch (ArgumentException e)
            {

                p.OnOutput(new OutputHandlerEventArgs("e: " + e.Message));
            }

            if (p.tsvFile != null)
            {
                p.OnOutput(new OutputHandlerEventArgs("Calibrating TSV file"));
                Calibrators.CalibrateTSV(bestCf, p);
            }
            p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
            List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
            p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
            List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);

            p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs);

            p.OnOutput(new OutputHandlerEventArgs("Finished running my software lock mass implementation"));
        }

        private static void WriteDataToFiles(IEnumerable<TrainingPoint> trainingPoints, string prefix)
        {
            using (StreamWriter file = new StreamWriter(prefix + @"MZ.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.mz);
            using (StreamWriter file = new StreamWriter(prefix + @"RT.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.rt);
            using (StreamWriter file = new StreamWriter(prefix + @"label.dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.l);
        }

    }
}