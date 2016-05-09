﻿using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class LinearCalibrationFunction : CalibrationFunction
    {
        private double a;
        private double b;
        private double c;

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

            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];
        }
    }
}