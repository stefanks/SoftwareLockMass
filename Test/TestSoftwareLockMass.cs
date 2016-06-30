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
            Assert.Throws<ArgumentException>(() => { new QuadraticCalibrationFunction(OnOutput, trainingList); }, "Not enough training points for Quadratic calibration function.Need at least 6, but have 5");
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
            SoftwareLockMassIO.IO.Load();

            SoftwareLockMassParams a = SoftwareLockMassIO.IO.GetReady(@"myFakeFile.mzML", P_outputHandler, P_progressHandler, P_outputHandler, @"myIdentifications.mzid");

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


        [Test]
        public void TestMathNetLinear()
        {
            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(1, 2), 40));
            trainingList.Add(new TrainingPoint(new DataPoint(11, 2), 10));
            trainingList.Add(new TrainingPoint(new DataPoint(430, 11), 540));
            trainingList.Add(new TrainingPoint(new DataPoint(3400, 151), 130));
            trainingList.Add(new TrainingPoint(new DataPoint(23451, 342), 4013));
            trainingList.Add(new TrainingPoint(new DataPoint(13411, 162), 410));
            trainingList.Add(new TrainingPoint(new DataPoint(4614330, 1311), 5540));
            trainingList.Add(new TrainingPoint(new DataPoint(3411300, 41151), 1630));
            new LinearCalibrationFunctionMathNet(OnOutput, trainingList);
            var a = new LinearCalibrationFunction(OnOutput, trainingList);
            Console.WriteLine("My linear prediction: " + a.Predict(new DataPoint(3, 4)));
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