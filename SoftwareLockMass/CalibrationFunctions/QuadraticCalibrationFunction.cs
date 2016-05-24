using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class QuadraticCalibrationFunction : CalibrationFunction
    {
        private double a;
        private double b;
        private double c;
        private double d;
        private double e;
        private double f;
        private Action<OutputHandlerEventArgs> onOutput;

        public QuadraticCalibrationFunction(Action<OutputHandlerEventArgs> onOutput)
        {
            this.onOutput = onOutput;
        }

        public override double Predict(DataPoint t)
        {
            return a + b * t.mz + c * t.rt + d*Math.Pow(t.mz,2) +e * Math.Pow(t.rt, 2) + f * t.mz*t.rt;
        }

        public override void Train(List<TrainingPoint> trainingList)
        {

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;
            
            var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithInterceptAndSquares()));
            var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

            var coeffs = X.Solve(y);

            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];
            d = coeffs[3];
            e = coeffs[4];
            f = coeffs[5];
        }
    }
}