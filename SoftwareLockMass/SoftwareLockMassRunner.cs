﻿using Spectra;
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

            p.OnOutput(new OutputHandlerEventArgs("Pre-calibration (Software Lock Mass):"));

            List<int> trainingPointCounts = new List<int>();
            List<int> calibrationValues = new List<int>();
            List<LabeledDataPoint> pointList;
            for (int preCalibraionRound = 0; ; preCalibraionRound++)
            {
                p.OnOutput(new OutputHandlerEventArgs("Pre-Calibration round " + preCalibraionRound));
                p.OnOutput(new OutputHandlerEventArgs("Getting Training Points"));

                pointList = TrainingPointsExtractor.GetTrainingPoints(p.myMsDataFile, p.identifications, p);

                if (preCalibraionRound >= 1 && pointList.Count <= trainingPointCounts[preCalibraionRound - 1])
                    break;

                trainingPointCounts.Add(pointList.Count);

                var pointList1 = pointList.Where((b) => b.inputs[0] == 1).ToList();
                WriteDataToFiles(pointList1, "pointList1" + p.myMsDataFile.Name + preCalibraionRound);
                p.OnOutput(new OutputHandlerEventArgs("pointList1.Count() = " + pointList1.Count()));
                var pointList2 = pointList.Where((b) => b.inputs[0] == 2).ToList();
                WriteDataToFiles(pointList2, "pointList2" + p.myMsDataFile.Name + preCalibraionRound);
                p.OnOutput(new OutputHandlerEventArgs("pointList2.Count() = " + pointList2.Count()));

                CalibrationFunction identityPredictor = new IdentityCalibrationFunction(p.OnOutput);
                p.OnOutput(new OutputHandlerEventArgs("Uncalibrated MSE, " + identityPredictor.getMSE(pointList1) + "," + identityPredictor.getMSE(pointList2) + "," + identityPredictor.getMSE(pointList)));

                CalibrationFunction ms1regressor = new ConstantCalibrationFunction(p.OnOutput, pointList1);
                CalibrationFunction ms2regressor = new ConstantCalibrationFunction(p.OnOutput, pointList2);
                CalibrationFunction combinedCalibration = new SeparateCalibrationFunction(ms1regressor, ms2regressor);

                p.OnOutput(new OutputHandlerEventArgs("Pre-Calibrating Spectra"));

                CalibrateSpectra(p, combinedCalibration);

                combinedCalibration.writeNewLabels(pointList1, "pointList1preCalibration" + p.myMsDataFile.Name + preCalibraionRound);
                combinedCalibration.writeNewLabels(pointList2, "pointList2preCalibration" + p.myMsDataFile.Name + preCalibraionRound);
                p.OnOutput(new OutputHandlerEventArgs("After constant shift MSE, " + ms1regressor.getMSE(pointList1) + "," + ms2regressor.getMSE(pointList2) + "," + combinedCalibration.getMSE(pointList)));



            }

            p.OnOutput(new OutputHandlerEventArgs("Actual Calibration"));

            CalibrationFunction cfForTSVcalibration = Calibrate(pointList, p);

            p.postProcessing(p);

            if (p.tsvFile != null)
            {
                p.OnOutput(new OutputHandlerEventArgs("Calibrating TSV file"));
                Calibrators.CalibrateTSV(cfForTSVcalibration, p);
            }

            p.OnOutput(new OutputHandlerEventArgs("Finished running my software lock mass implementation"));
            p.OnProgress(new ProgressHandlerEventArgs(0));
        }

        private static CalibrationFunction Calibrate(List<LabeledDataPoint> trainingPoints, SoftwareLockMassParams p)
        {
            var rnd = new Random();
            var shuffledTrainingPoints = trainingPoints.OrderBy(item => rnd.Next()).ToArray();

            var trainList = shuffledTrainingPoints.Take(trainingPoints.Count * 3 / 4).ToList();
            var testList = shuffledTrainingPoints.Skip(trainingPoints.Count * 3 / 4).ToList();

            var trainList1 = trainList.Where((b) => b.inputs[0] == 1).ToList();
            WriteDataToFiles(trainList1, "train1" + p.myMsDataFile.Name);
            p.OnOutput(new OutputHandlerEventArgs("trainList1.Count() = " + trainList1.Count()));
            var trainList2 = trainList.Where((b) => b.inputs[0] == 2).ToList();
            WriteDataToFiles(trainList2, "train2" + p.myMsDataFile.Name);
            p.OnOutput(new OutputHandlerEventArgs("trainList2.Count() = " + trainList2.Count()));
            var testList1 = testList.Where((b) => b.inputs[0] == 1).ToList();
            WriteDataToFiles(testList1, "test1" + p.myMsDataFile.Name);
            p.OnOutput(new OutputHandlerEventArgs("testList1.Count() = " + testList1.Count()));
            var testList2 = testList.Where((b) => b.inputs[0] == 2).ToList();
            WriteDataToFiles(testList2, "test2" + p.myMsDataFile.Name);
            p.OnOutput(new OutputHandlerEventArgs("testList2.Count() = " + testList2.Count()));

            CalibrationFunction bestMS1predictor = new IdentityCalibrationFunction(p.OnOutput);
            CalibrationFunction bestMS2predictor = new IdentityCalibrationFunction(p.OnOutput);
            CalibrationFunction combinedCalibration = new SeparateCalibrationFunction(bestMS1predictor, bestMS2predictor);
            double bestMS1MSE = bestMS1predictor.getMSE(testList1);
            double bestMS2MSE = bestMS2predictor.getMSE(testList2);
            double combinedMSE = combinedCalibration.getMSE(testList);
            p.OnOutput(new OutputHandlerEventArgs("Uncalibrated MSE, " + bestMS1MSE + "," + bestMS2MSE + "," + combinedMSE));

            CalibrationFunction ms1regressor = new ConstantCalibrationFunction(p.OnOutput, trainList1);
            CalibrationFunction ms2regressor = new ConstantCalibrationFunction(p.OnOutput, trainList2);
            combinedCalibration = new SeparateCalibrationFunction(ms1regressor, ms2regressor);
            combinedCalibration.writeNewLabels(trainList1, "trainList1Constant" + p.myMsDataFile.Name);
            combinedCalibration.writeNewLabels(trainList2, "trainList2Constant" + p.myMsDataFile.Name);
            combinedCalibration.writeNewLabels(testList1, "testList1Constant" + p.myMsDataFile.Name);
            combinedCalibration.writeNewLabels(testList2, "testList2Constant" + p.myMsDataFile.Name);
            double MS1mse = ms1regressor.getMSE(testList1);
            double MS2mse = ms2regressor.getMSE(testList2);
            combinedMSE = combinedCalibration.getMSE(testList);
            p.OnOutput(new OutputHandlerEventArgs("Constant calibration MSE, " + MS1mse + "," + MS2mse + "," + combinedMSE));
            if (MS1mse < bestMS1MSE)
            {
                bestMS1MSE = MS1mse;
                bestMS1predictor = ms1regressor;
            }
            if (MS2mse < bestMS2MSE)
            {
                bestMS2MSE = MS2mse;
                bestMS2predictor = ms2regressor;
            }

            List<bool[]> featuresArray = new List<bool[]>();
            featuresArray.Add(new bool[6] { false, true, false, false, false, false });
            featuresArray.Add(new bool[6] { false, false, true, false, false, false });
            featuresArray.Add(new bool[6] { false, false, false, true, false, false });
            featuresArray.Add(new bool[6] { false, false, false, false, true, false });
            featuresArray.Add(new bool[6] { false, false, false, false, false, true });

            featuresArray.Add(new bool[6] { false, true, true, false, false, false });
            featuresArray.Add(new bool[6] { false, true, false, true, false, false });
            featuresArray.Add(new bool[6] { false, true, false, false, true, false });
            featuresArray.Add(new bool[6] { false, true, false, false, false, true });
            featuresArray.Add(new bool[6] { false, false, true, true, false, false });
            featuresArray.Add(new bool[6] { false, false, true, false, true, false });
            featuresArray.Add(new bool[6] { false, false, true, false, false, true });
            featuresArray.Add(new bool[6] { false, false, false, true, true, false });
            featuresArray.Add(new bool[6] { false, false, false, true, false, true });
            featuresArray.Add(new bool[6] { false, false, false, false, true, true });

            featuresArray.Add(new bool[6] { false, false, false, true, true, true });
            featuresArray.Add(new bool[6] { false, false, true, false, true, true });
            featuresArray.Add(new bool[6] { false, false, true, true, false, true });
            featuresArray.Add(new bool[6] { false, false, true, true, true, false });
            featuresArray.Add(new bool[6] { false, true, false, false, true, true });
            featuresArray.Add(new bool[6] { false, true, false, true, false, true });
            featuresArray.Add(new bool[6] { false, true, false, true, true, false });
            featuresArray.Add(new bool[6] { false, true, true, false, false, true });
            featuresArray.Add(new bool[6] { false, true, true, false, true, false });
            featuresArray.Add(new bool[6] { false, true, true, true, false, false });

            featuresArray.Add(new bool[6] { false, false, true, true, true, true });
            featuresArray.Add(new bool[6] { false, true, false, true, true, true });
            featuresArray.Add(new bool[6] { false, true, true, false, true, true });
            featuresArray.Add(new bool[6] { false, true, true, true, false, true });
            featuresArray.Add(new bool[6] { false, true, true, true, true, false });

            featuresArray.Add(new bool[6] { false, true, true, true, true, true });

            List<bool[]> logArray = new List<bool[]>();

            logArray.Add(new bool[6] { false, false, false, true, true, true });
            //logArray.Add(new bool[6] { false, false, false, false, true, true });
            //logArray.Add(new bool[6] { false, false, false, true, false, true });
            //logArray.Add(new bool[6] { false, false, false, true, true, false });
            //logArray.Add(new bool[6] { false, false, false, true, false, false });
            //logArray.Add(new bool[6] { false, false, false, false, true, false });
            //logArray.Add(new bool[6] { false, false, false, false, false, true });
            //logArray.Add(new bool[6] { false, false, false, false, false, false });

            foreach (var logVars in logArray)
            {
                foreach (var ok in featuresArray)
                {
                    ms1regressor = new LinearCalibrationFunctionMathNet(p.OnOutput, trainList1, ok, logVars);
                    ms2regressor = new LinearCalibrationFunctionMathNet(p.OnOutput, trainList2, ok, logVars);
                    combinedCalibration = new SeparateCalibrationFunction(ms1regressor, ms2regressor);
                    combinedCalibration.writeNewLabels(trainList1, "trainList1Linear" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    combinedCalibration.writeNewLabels(trainList2, "trainList2Linear" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    combinedCalibration.writeNewLabels(testList1, "testList1Linear" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    combinedCalibration.writeNewLabels(testList2, "testList2Linear" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    MS1mse = ms1regressor.getMSE(testList1);
                    MS2mse = ms2regressor.getMSE(testList2);
                    combinedMSE = combinedCalibration.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("Linear calibration " + string.Join("", ok) + string.Join("", logVars) + " MSE, " + MS1mse + "," + MS2mse + "," + combinedMSE));
                    if (MS1mse < bestMS1MSE)
                    {
                        bestMS1MSE = MS1mse;
                        bestMS1predictor = ms1regressor;
                    }
                    if (MS2mse < bestMS2MSE)
                    {
                        bestMS2MSE = MS2mse;
                        bestMS2predictor = ms2regressor;
                    }
                }
            }

            foreach (var logVars in logArray)
            {
                foreach (var ok in featuresArray)
                {
                    ms1regressor = new QuadraticCalibrationFunctionMathNet(p.OnOutput, trainList1, ok, logVars);
                    ms2regressor = new QuadraticCalibrationFunctionMathNet(p.OnOutput, trainList2, ok, logVars);
                    combinedCalibration = new SeparateCalibrationFunction(ms1regressor, ms2regressor);
                    combinedCalibration.writeNewLabels(trainList1, "trainList1Quadratic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    combinedCalibration.writeNewLabels(trainList2, "trainList2Quadratic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    combinedCalibration.writeNewLabels(testList1, "testList1Quadratic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    combinedCalibration.writeNewLabels(testList2, "testList2Quadratic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
                    MS1mse = ms1regressor.getMSE(testList1);
                    MS2mse = ms2regressor.getMSE(testList2);
                    combinedMSE = combinedCalibration.getMSE(testList);
                    p.OnOutput(new OutputHandlerEventArgs("Quadratic calibration " + string.Join("", ok) + string.Join("", logVars) + " MSE, " + MS1mse + "," + MS2mse + "," + combinedMSE));
                    if (MS1mse < bestMS1MSE)
                    {
                        bestMS1MSE = MS1mse;
                        bestMS1predictor = ms1regressor;
                    }
                    if (MS2mse < bestMS2MSE)
                    {
                        bestMS2MSE = MS2mse;
                        bestMS2predictor = ms2regressor;
                    }
                }
            }

            //foreach (var logVars in logArray)
            //{
            //    foreach (var ok in featuresArray)
            //    {
            //        ms1regressor = new CubicCalibrationFunctionMathNet(p.OnOutput, trainList1, ok, logVars);
            //        ms2regressor = new CubicCalibrationFunctionMathNet(p.OnOutput, trainList2, ok, logVars);
            //        combinedCalibration = new SeparateCalibrationFunction(ms1regressor, ms2regressor);
            //        combinedCalibration.writeNewLabels(trainList1, "trainList1Cubic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
            //        combinedCalibration.writeNewLabels(trainList2, "trainList2Cubic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
            //        combinedCalibration.writeNewLabels(testList1, "testList1Cubic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
            //        combinedCalibration.writeNewLabels(testList2, "testList2Cubic" + string.Join("", ok) + string.Join("", logVars) + p.myMsDataFile.Name);
            //        MS1mse = ms1regressor.getMSE(testList1);
            //        MS2mse = ms2regressor.getMSE(testList2);
            //        combinedMSE = combinedCalibration.getMSE(testList);
            //        p.OnOutput(new OutputHandlerEventArgs("Cubic calibration " + string.Join("", ok) + string.Join("", logVars) + " MSE, " + MS1mse + "," + MS2mse + "," + combinedMSE));
            //        if (MS1mse < bestMS1MSE)
            //        {
            //            bestMS1MSE = MS1mse;
            //            bestMS1predictor = ms1regressor;
            //        }
            //        if (MS2mse < bestMS2MSE)
            //        {
            //            bestMS2MSE = MS2mse;
            //            bestMS2predictor = ms2regressor;
            //        }
            //    }
            //}

            CalibrationFunction bestCf = new SeparateCalibrationFunction(bestMS1predictor, bestMS2predictor);

            p.OnOutput(new OutputHandlerEventArgs("Calibrating Spectra"));

            CalibrateSpectra(p, bestCf);

            return bestCf;
        }

        private static void CalibrateSpectra(SoftwareLockMassParams p, CalibrationFunction bestCf)
        {
            foreach (var a in p.myMsDataFile)
            {
                if (a.MsnOrder == 2)
                {
                    int precursorScanNumber;
                    a.TryGetPrecursorScanNumber(out precursorScanNumber);
                    var precursorScan = p.myMsDataFile.GetScan(precursorScanNumber);

                    double precursorMZ;
                    a.TryGetSelectedIonGuessMZ(out precursorMZ);
                    double precursorIntensity;
                    a.TryGetSelectedIonGuessIntensity(out precursorIntensity);
                    double newSelectedMZ = precursorMZ - bestCf.Predict(new double[6] { 1, precursorMZ, precursorScan.RetentionTime, precursorIntensity, precursorScan.TotalIonCurrent, precursorScan.InjectionTime });

                    
                    double monoisotopicMZ;
                    a.TryGetSelectedIonGuessMonoisotopicMZ(out monoisotopicMZ);
                    double monoisotopicIntensity;
                    a.TryGetSelectedIonGuessMonoisotopicIntensity(out monoisotopicIntensity);
                    double newMonoisotopicMZ = monoisotopicMZ - bestCf.Predict(new double[6] { 1, monoisotopicMZ, precursorScan.RetentionTime, monoisotopicIntensity, precursorScan.TotalIonCurrent, precursorScan.InjectionTime });

                    

                    int SelectedIonGuessChargeStateGuess;
                    a.TryGetSelectedIonGuessChargeStateGuess(out SelectedIonGuessChargeStateGuess);
                    double IsolationMZ;
                    a.TryGetIsolationMZ(out IsolationMZ);

                    Func<MzPeak, double> theFunc = x => x.MZ - bestCf.Predict(new double[9] { 2, x.MZ, a.RetentionTime, x.Intensity, a.TotalIonCurrent, a.InjectionTime, SelectedIonGuessChargeStateGuess, IsolationMZ, (x.MZ - a.ScanWindowRange.Minimum) / (a.ScanWindowRange.Maximum - a.ScanWindowRange.Minimum) });
                    a.tranformByApplyingFunctionsToSpectraAndReplacingPrecursorMZs(theFunc, newSelectedMZ, newMonoisotopicMZ);
                }
                else
                {
                    Func<MzPeak, double> theFUnc = x => x.MZ - bestCf.Predict(new double[6] { 1, x.MZ, a.RetentionTime, x.Intensity, a.TotalIonCurrent, a.InjectionTime });
                    a.tranformByApplyingFunctionsToSpectraAndReplacingPrecursorMZs(theFUnc, double.NaN, double.NaN);
                }
            }
        }

        private static void WriteDataToFiles(IEnumerable<LabeledDataPoint> trainingPoints, string prefix)
        {
            using (StreamWriter file = new StreamWriter(prefix + ".dat"))
            {
                if (trainingPoints.First().inputs.Count() == 9)
                    file.WriteLine("MS, MZ, RetentionTime, Intensity, TotalIonCurrent, InjectionTime, SelectedIonGuessChargeStateGuess, IsolationMZ, relativeMZ, label");
                else
                    file.WriteLine("MS, MZ, RetentionTime, Intensity, TotalIonCurrent, InjectionTime, label");
                foreach (LabeledDataPoint d in trainingPoints)
                    file.WriteLine(string.Join(",", d.inputs) + "," + d.output);
            }
        }
    }
}