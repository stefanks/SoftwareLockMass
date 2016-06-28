using NUnit.Framework;
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
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            EventHandler<OutputHandlerEventArgs> p_outputHandler = P_outputHandler;
            outputHandler += p_outputHandler;
            EventHandler<ProgressHandlerEventArgs> p_progressHandler = P_progressHandler;

        }

        [Test]
        public void TestQuadraticCalibration()
        {
            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(600, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(500, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(400, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(300, 1), 0));
            Assert.Throws<ArgumentException>(() => { new QuadraticCalibrationFunction(OnOutput,trainingList); }, "Not enough training points for Quadratic calibration function.Need at least 6, but have 5");
        }

        [Test]
        public void TesLinearCalibrationOK()
        {
            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(134, 13251), 4130));
            trainingList.Add(new TrainingPoint(new DataPoint(5132400, 43211), 12350));
            trainingList.Add(new TrainingPoint(new DataPoint(4413200, 1314), 65430));
            trainingList.Add(new TrainingPoint(new DataPoint(302450, 451), 26));
            LinearCalibrationFunction cf = new LinearCalibrationFunction(OnOutput, trainingList);
        }



        [Test]
        public void TestSoftwareLockMassRunner()
        {
            double intensityCutoff = 0;
            double toleranceInMZforSearch = 0.032;

            SoftwareLockMassIO.IO.Load();

            SoftwareLockMassParams a = SoftwareLockMassIO.IO.GetReady(@"myFile.mzML", P_outputHandler, P_progressHandler, P_outputHandler, @"myIdentifications.mzid", intensityCutoff, toleranceInMZforSearch);

            SoftwareLockMassRunner.Run(a);
        }



        [Test]
        public void TesLinearCalibration()
        {

            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(600, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(500, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(400, 1), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(300, 1), 0));
            Assert.Throws<ArgumentException>(() => { new LinearCalibrationFunction(OnOutput, trainingList); }, "Could not train LinearCalibrationFunction, data might be low rank");

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
        private static void P_progressHandler(object sender, ProgressHandlerEventArgs e)
        {
            Console.Write(e.progress + "% ");
        }
    }
}