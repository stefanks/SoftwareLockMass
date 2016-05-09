using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class QuarticCalibrationFunction : CalibrationFunction
    {
        private double a;
        private double b;
        private double c;

        private double d;
        private double e;
        private double f;

        private double g;
        private double h;
        private double i;
        private double j;

        private double k;
        private double l;
        private double m;
        private double n;
        private double o;

        public override double Predict(DataPoint t)
        {
            return a + b * t.mz + c * t.rt + d * Math.Pow(t.mz, 2) + e * Math.Pow(t.rt, 2) + f * t.mz * t.rt +
                g * Math.Pow(t.mz, 3) +
                h * Math.Pow(t.mz, 2) * t.rt +
                i * t.mz * Math.Pow(t.rt, 2) +
                j * Math.Pow(t.rt, 3) +
                k * Math.Pow(t.mz, 4) +
                l * Math.Pow(t.mz, 3) * t.rt +
                m * Math.Pow(t.mz, 2) * Math.Pow(t.rt, 2) +
                n * t.mz * Math.Pow(t.rt, 3) +
                o * Math.Pow(t.rt, 4);
        }

        public override void Train(List<TrainingPoint> trainingList)
        {

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;
            
            var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithInterceptAndSquaresAndCubesAndQuarts()));
            var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

            var coeffs = X.Solve(y);

            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];
            d = coeffs[3];
            e = coeffs[4];
            f = coeffs[5];
            g = coeffs[6];
            h = coeffs[7];
            i = coeffs[8];
            j = coeffs[9];
            k = coeffs[10];
            l = coeffs[11];
            m = coeffs[12];
            n = coeffs[13];
            o = coeffs[14];
        }
    }
}