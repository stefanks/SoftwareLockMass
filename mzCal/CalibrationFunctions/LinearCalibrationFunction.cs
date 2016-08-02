//using MathNet.Numerics.LinearAlgebra;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace SoftwareLockMass
//{
//    public class LinearCalibrationFunction : CalibrationFunction
//    {
//        private double a;
//        private double b;
//        private double c;
//        private Action<OutputHandlerEventArgs> onOutput;


//        public LinearCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, IEnumerable<LabeledDataPoint> trainingList)
//        {
//            this.onOutput = onOutput;
//            Train(trainingList);
//        }

//        public override double Predict(double[] t)
//        {
//            return a + b *t[1]  + c * t[2];
//        }

//        private void Train(IEnumerable<LabeledDataPoint> trainingList)
//        {

//            //var M = Matrix<double>.Build;
//            //var V = Vector<double>.Build;

//            //var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithIntercept()));
//            //var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

//            //var coeffs = X.Solve(y);

//            //if (double.IsNaN(coeffs[0]))
//            //    throw new ArgumentException("Could not train LinearCalibrationFunction, data might be low rank");

//            //a = coeffs[0];
//            //b = coeffs[1];
//            //c = coeffs[2];
//            onOutput(new OutputHandlerEventArgs("Sucessfully trained LinearCalibrationFunction:"));

//        }
//    }
//}