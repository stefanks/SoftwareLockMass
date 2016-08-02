using NUnit.Framework;
using mzCal;
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
            trainingList.Add(new TrainingPoint(new DataPoint(600, 1, 1, 1, double.NaN, double.NaN), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(500, 1, 1, 1, double.NaN, double.NaN), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(400, 1, 1, 1, double.NaN, double.NaN), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(300, 1, 1, 1, double.NaN, double.NaN), 0));
            Assert.Throws<ArgumentException>(() => { new QuadraticCalibrationFunction(OnOutput, trainingList); }, "Not enough training points for Quadratic calibration function.Need at least 6, but have 5");
        }

        [Test]
        public void TesLinearCalibrationOK()
        {
            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(134, 13251, 1, 1, double.NaN, double.NaN), 4130));
            trainingList.Add(new TrainingPoint(new DataPoint(5132400, 43211, 1, 1, double.NaN, double.NaN), 12350));
            trainingList.Add(new TrainingPoint(new DataPoint(4413200, 1314, 1, 1, double.NaN, double.NaN), 65430));
            trainingList.Add(new TrainingPoint(new DataPoint(302450, 451, 1, 1, double.NaN, double.NaN), 26));
            //LinearCalibrationFunction cf = new LinearCalibrationFunction(OnOutput, trainingList);
        }



        [Test]
        public void TestSoftwareLockMassRunner()
        {
            mzCalIO.IO.Load();

            SoftwareLockMassParams a = mzCalIO.IO.GetReady(@"myFakeFile.mzML", P_outputHandler, P_progressHandler, P_outputHandler, @"myIdentifications.mzid", true);

            SoftwareLockMassRunner.Run(a);
        }



        [Test]
        public void TesLinearCalibration()
        {

            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(600, 1, 1, 1, double.NaN, double.NaN), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(500, 1, 1, 1, double.NaN, double.NaN), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(400, 1, 1, 1, double.NaN, double.NaN), 0));
            trainingList.Add(new TrainingPoint(new DataPoint(300, 1, 1, 1, double.NaN, double.NaN), 0));
            //Assert.Throws<ArgumentException>(() => { new LinearCalibrationFunction(OnOutput, trainingList); }, "Could not train LinearCalibrationFunction, data might be low rank");

        }


        [Test]
        public void TestMathNetLinear()
        {
            List<TrainingPoint> trainingList = new List<TrainingPoint>();
            trainingList.Add(new TrainingPoint(new DataPoint(1, 2, 1, 1, double.NaN, double.NaN), 40));
            trainingList.Add(new TrainingPoint(new DataPoint(11, 2, 1, 1, double.NaN, double.NaN), 10));
            trainingList.Add(new TrainingPoint(new DataPoint(430, 11, 1, 1, double.NaN, double.NaN), 540));
            trainingList.Add(new TrainingPoint(new DataPoint(3400, 151, 1, 1, double.NaN, double.NaN), 130));
            trainingList.Add(new TrainingPoint(new DataPoint(23451, 342, 1, 1, double.NaN, double.NaN), 4013));
            trainingList.Add(new TrainingPoint(new DataPoint(13411, 162, 1, 1, double.NaN, double.NaN), 410));
            trainingList.Add(new TrainingPoint(new DataPoint(4614330, 1311, 1, 1, double.NaN, double.NaN), 5540));
            trainingList.Add(new TrainingPoint(new DataPoint(3411300, 41151, 1, 1, double.NaN, double.NaN), 1630));
            //new LinearCalibrationFunctionMathNet(OnOutput, trainingList);
            //var a = new LinearCalibrationFunction(OnOutput, trainingList);
        }

        event EventHandler<OutputHandlerEventArgs> outputHandler;

        public void OnOutput(OutputHandlerEventArgs e)
        {
            var handler = this.outputHandler;
            if (handler != null)
                handler(this, e);
        }

        private static void P_outputHandler(object sender, OutputHandlerEventArgs e)
        {
            Console.WriteLine(e.output);
        }
        private static void P_progressHandler(object sender, ProgressHandlerEventArgs e)
        {
            Console.Write(e.progress + "% ");
        }



        [Test]
        public void TestConstantCalibration()
        {
            List<LabeledDataPoint> trainingList = new List<LabeledDataPoint>();
            trainingList.Add(new LabeledDataPoint(new double[3] { 1, 1, 1 }, 0.5));
            trainingList.Add(new LabeledDataPoint(new double[3] { 1, 2, 1 }, 0.5));
            trainingList.Add(new LabeledDataPoint(new double[3] { 2, 1, 1 }, 0.5));
            trainingList.Add(new LabeledDataPoint(new double[3] { 2,2, 1 }, 0.5));
            IdentityCalibrationFunction cf = new IdentityCalibrationFunction(OnOutput);
            Assert.AreEqual(4 * Math.Pow(0.5, 2) / 4, cf.getMSE(trainingList));
            ConstantCalibrationFunction cfconst = new ConstantCalibrationFunction(OnOutput, trainingList);
            Assert.AreEqual(0, cfconst.getMSE(trainingList));
        }
    }
}