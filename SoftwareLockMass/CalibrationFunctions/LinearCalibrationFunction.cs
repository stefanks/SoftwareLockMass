using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    public class LinearCalibrationFunction : CalibrationFunction
    {
        private double a;
        private double b;
        private double c;
        private Action<OutputHandlerEventArgs> onOutput;

        public LinearCalibrationFunction(Action<OutputHandlerEventArgs> onOutput)
        {
            this.onOutput = onOutput;
        }

        public override double Predict(DataPoint t)
        {
            return a + b * t.mz + c * t.rt;
        }

        public override void Train(List<TrainingPoint> trainingList)
        {

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithIntercept()));
            var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

            var coeffs = X.Solve(y);

            if (double.IsNaN(coeffs[0]))
                throw new ArgumentException("Could not train LinearCalibrationFunction, data might be low rank");

            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];
            onOutput(new OutputHandlerEventArgs("Sucessfully trained LinearCalibrationFunction:"));
            onOutput(new OutputHandlerEventArgs("a =" + a + " b =" + b + " c =" + c));

        }
    }
}