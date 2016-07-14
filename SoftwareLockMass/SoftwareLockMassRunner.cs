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

            for (int outerLoopIndex = 1; outerLoopIndex <= 4; outerLoopIndex++)
            {
                p.OnOutput(new OutputHandlerEventArgs("Calibration round " + outerLoopIndex));
                p.OnOutput(new OutputHandlerEventArgs("Getting Training Points"));
                List<TrainingPoint> trainingPoints = TrainingPointsExtractor.GetTrainingPoints(p.myMsDataFile, p.identifications, p);

                p.OnOutput(new OutputHandlerEventArgs("Writing training points to file"));
                WriteDataToFiles(trainingPoints, "all", outerLoopIndex);

                var rnd = new Random();
                var shuffledTrainingPoints = trainingPoints.OrderBy(item => rnd.Next()).ToArray();

                var trainList = shuffledTrainingPoints.Take(trainingPoints.Count * 3 / 4).ToArray();
                var testList = shuffledTrainingPoints.Skip(trainingPoints.Count * 3 / 4).ToArray();

                var trainList1 = trainList.Where((b) => b.dp.msnOrder == 1).ToArray();
                WriteDataToFiles(trainList1, "train1", outerLoopIndex);
                var trainList2 = trainList.Where((b) => b.dp.msnOrder == 2).ToArray();
                WriteDataToFiles(trainList2, "train2", outerLoopIndex);

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
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "const");
                    }

                    cf = new SeparateCalibrationFunction(new ConstantCalibrationFunction(p.OnOutput, trainList1), new ConstantCalibrationFunction(p.OnOutput, trainList2));

                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Constant separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "constsep");
                    }

                    cf = new LinearCalibrationFunction(p.OnOutput, trainList);
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Linear: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "lin");
                    }

                    cf = new SeparateCalibrationFunction(new LinearCalibrationFunction(p.OnOutput, trainList1), new LinearCalibrationFunction(p.OnOutput, trainList2));

                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Linear separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "linsep");
                    }

                    cf = new QuadraticCalibrationFunction(p.OnOutput, trainList);
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Quadratic: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "second");
                    }

                    cf = new SeparateCalibrationFunction(new QuadraticCalibrationFunction(p.OnOutput, trainList1), new QuadraticCalibrationFunction(p.OnOutput, trainList2));

                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Quadratic separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "secondsep");
                    }


                    cf = new CubicCalibrationFunction(p.OnOutput, trainList);
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Cubic: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "third");
                    }


                    cf = new SeparateCalibrationFunction(new CubicCalibrationFunction(p.OnOutput, trainList1), new CubicCalibrationFunction(p.OnOutput, trainList2));

                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Cubic separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "thirdsep");
                    }


                    cf = new QuarticCalibrationFunction(p.OnOutput, trainList);
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Quartic: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "fourth");
                    }

                    cf = new SeparateCalibrationFunction(new QuarticCalibrationFunction(p.OnOutput, trainList1), new QuarticCalibrationFunction(p.OnOutput, trainList2));

                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Quartic separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "fourthsep");
                    }


                    cf = new FifthCalibrationFunction(p.OnOutput, trainList);
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Fifth: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "fifth");
                    }

                    cf = new SeparateCalibrationFunction(new FifthCalibrationFunction(p.OnOutput, trainList1), new FifthCalibrationFunction(p.OnOutput, trainList2));

                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Fifth separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "fifthsep");
                    }

                    cf = new SixthCalibrationFunction(p.OnOutput, trainList);
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Sixth: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "sixth");
                    }

                    cf = new SeparateCalibrationFunction(new SixthCalibrationFunction(p.OnOutput, trainList1), new SixthCalibrationFunction(p.OnOutput, trainList2));

                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE Sixth separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "sixthsep");
                    }

                    cf = new KDTreeCalibrationFunction(p.OnOutput, trainList);
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE KD Tree: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "kdtree");
                    }

                    cf = new SeparateCalibrationFunction(new KDTreeCalibrationFunction(p.OnOutput, trainList1), new KDTreeCalibrationFunction(p.OnOutput, trainList2));
                    mse = cf.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("MSE KD Tree separate: " + mse));
                    if (mse < bestMSE)
                    {
                        bestMSE = mse;
                        bestCf = cf;
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                        //List<IMzSpectrum<MzPeak>> calibratedSpectra = Calibrators.CalibrateSpectra(bestCf, p);
                        //p.OnOutput(new OutputHandlerEventArgs("Calibrating Precursor MZs"));
                        //List<double> calibratedPrecursorMZs = Calibrators.CalibratePrecursorMZs(bestCf, p);
                        //p.postProcessing(p, calibratedSpectra, calibratedPrecursorMZs, "kdtreesep");
                    }
                }
                catch (ArgumentException e)
                {

                    p.OnOutput(new OutputHandlerEventArgs("e: " + e.Message));
                }
                p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));
                foreach (var a in p.myMsDataFile)
                {
                    if (a.MsnOrder == 2)
                    {
                        int precursorScanNumber;
                        a.TryGetPrecursorScanNumber(out precursorScanNumber);
                        double precursorTime = p.myMsDataFile.GetScan(precursorScanNumber).RetentionTime;
                        a.tranformByApplyingFunctionToX(x => x - bestCf.Predict(new DataPoint(x, a.RetentionTime, a.MsnOrder)), x => x - bestCf.Predict(new DataPoint(x, precursorTime, 1)));
                    }
                    else
                        a.tranformByApplyingFunctionToX(x => x - bestCf.Predict(new DataPoint(x, a.RetentionTime, a.MsnOrder)), x => 0);

                }


                p.postProcessing(p, outerLoopIndex.ToString());

                if (p.tsvFile != null)
                {
                    p.OnOutput(new OutputHandlerEventArgs("Calibrating TSV file"));
                    Calibrators.CalibrateTSV(bestCf, p);
                }
            }


            p.OnOutput(new OutputHandlerEventArgs("Finished running my software lock mass implementation"));
        }

        private static void WriteDataToFiles(IEnumerable<TrainingPoint> trainingPoints, string prefix, int suffix)
        {
            using (StreamWriter file = new StreamWriter(prefix + @"MZ" + suffix + ".dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.mz);
            using (StreamWriter file = new StreamWriter(prefix + @"RT" + suffix + ".dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.dp.rt);
            using (StreamWriter file = new StreamWriter(prefix + @"label" + suffix + ".dat"))
                foreach (TrainingPoint d in trainingPoints)
                    file.WriteLine(d.l);
        }

    }
}