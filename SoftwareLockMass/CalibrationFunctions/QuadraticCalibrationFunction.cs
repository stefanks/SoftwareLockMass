using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    public class QuadraticCalibrationFunction : CalibrationFunction
    {
        private double a;
        private double b;
        private double c;
        private double d;
        private double e;
        private double f;
        private Action<OutputHandlerEventArgs> onOutput;

        public QuadraticCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, IEnumerable<TrainingPoint> trainingList)
        {
            this.onOutput = onOutput;
            Train(trainingList);
        }

        public override double Predict(double[] t)
        {
            return a + b * t[1] + c * t[2] + d * Math.Pow(t[1], 2) + e * Math.Pow(t[2], 2) + f * t[1] * t[2];
        }

        public void Train(IEnumerable<TrainingPoint> trainingList)
        {

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithInterceptAndSquares()));
            var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

            Vector<double> coeffs;
            try
            {
                coeffs = X.Solve(y);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Not enough training points for Quadratic calibration function. Need at least 6, but have " + trainingList.Count());
            }
            if (double.IsNaN(coeffs[0]))
                throw new ArgumentException("Could not train QuadraticCalibrationFunction, data might be low rank");

            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];
            d = coeffs[3];
            e = coeffs[4];
            f = coeffs[5];
            onOutput(new OutputHandlerEventArgs("Sucessfully trained QuadraticCalibrationFunction"));
        }
    }
}