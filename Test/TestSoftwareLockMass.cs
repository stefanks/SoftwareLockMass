﻿using NUnit.Framework;
using SoftwareLockMass;
using System;
using System.Collections.Generic;

namespace Test
{
    [TestFixture]
    public sealed class TestSoftwareLockMass
    {

        [OneTimeSetUp]
        public void setup()
        {
            EventHandler<OutputHandlerEventArgs> p_outputHandler = P_outputHandler;
            outputHandler += p_outputHandler;

        }

        [Test]
        public void TestQuadraticCalibration()
        {
            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(600, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(500, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(400, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(300, 1), 0));
            QuadraticCalibrationFunction cf = new QuadraticCalibrationFunction(OnOutput);
            Assert.Throws<ArgumentException>(() => { cf.Train(trainingList); }, "Not enough training points for Quadratic calibration function.Need at least 6, but have 5");
        }

        [Test]
        public void TesLinearCalibrationOK()
        {
            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(134, 13251), 4130));
            trainingList.Add(new TrainingPoint(new DataPoint(5132400, 43211), 12350));
            trainingList.Add(new TrainingPoint(new DataPoint(4413200, 1314), 65430));
            trainingList.Add(new TrainingPoint(new DataPoint(302450, 451), 26));
            LinearCalibrationFunction cf = new LinearCalibrationFunction(OnOutput);
            cf.Train(trainingList);
        }

        [Test]
        public void TesLinearCalibration()
        {

            EventHandler<OutputHandlerEventArgs> p_outputHandler = P_outputHandler;
            outputHandler += p_outputHandler;

            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(600, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(500, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(400, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(300, 1), 0));
            LinearCalibrationFunction cf = new LinearCalibrationFunction(OnOutput);
            Assert.Throws<ArgumentException>(() => { cf.Train(trainingList); }, "Could not train LinearCalibrationFunction, data might be low rank");

        }

        event EventHandler<OutputHandlerEventArgs> outputHandler;

        public void OnOutput(OutputHandlerEventArgs e)
        {
            outputHandler?.Invoke(this, e);
        }

        private static void P_outputHandler(object sender, OutputHandlerEventArgs e)
        {
            Console.WriteLine(e.output);
        }
    }
}