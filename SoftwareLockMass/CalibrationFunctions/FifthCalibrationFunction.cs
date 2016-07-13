﻿using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class FifthCalibrationFunction : CalibrationFunction
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

        private double p;
        private double q;
        private double r;
        private double s;
        private double t;
        private double u;



        private Action<OutputHandlerEventArgs> onOutput;

        public FifthCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, IEnumerable<TrainingPoint> trainingList)
        {
            this.onOutput = onOutput;
            Train(trainingList);
        }

        public override double Predict(DataPoint dp)
        {
            return a + b * dp.mz + c * dp.rt + d * Math.Pow(dp.mz, 2) + e * Math.Pow(dp.rt, 2) + f * dp.mz * dp.rt +
                g * Math.Pow(dp.mz, 3) +
                h * Math.Pow(dp.mz, 2) * dp.rt +
                i * dp.mz * Math.Pow(dp.rt, 2) +
                j * Math.Pow(dp.rt, 3) +
                k * Math.Pow(dp.mz, 4) +
                l * Math.Pow(dp.mz, 3) * dp.rt +
                m * Math.Pow(dp.mz, 2) * Math.Pow(dp.rt, 2) +
                n * dp.mz * Math.Pow(dp.rt, 3) +
                o * Math.Pow(dp.rt, 4) +
                p * Math.Pow(dp.mz, 5) +
                q * Math.Pow(dp.mz, 4) * dp.rt +
                r * Math.Pow(dp.mz, 3) * Math.Pow(dp.rt, 2) +
                s * Math.Pow(dp.mz, 2) * Math.Pow(dp.rt, 3) +
                t * dp.mz * Math.Pow(dp.rt, 4) +
                u * Math.Pow(dp.rt, 5);
        }

        public void Train(IEnumerable<TrainingPoint> trainingList)
        {

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithInterceptAndSquaresAndCubesAndQuartsAndFifths()));
            var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

            var coeffs = X.Solve(y);
            if (double.IsNaN(coeffs[0]))
                throw new ArgumentException("Could not train FifthCalibrationFunction, data might be low rank");

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
            p = coeffs[15];
            q = coeffs[16];
            r = coeffs[17];
            s = coeffs[18];
            t = coeffs[19];
            u = coeffs[20];

            onOutput(new OutputHandlerEventArgs("Sucessfully trained FifthCalibrationFunction"));
        }
    }
}