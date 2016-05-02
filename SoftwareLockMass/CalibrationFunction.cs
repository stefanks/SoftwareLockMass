using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace SoftwareLockMass
{
    internal class CalibrationFunction
    {
        private List<double> labelData;
        private List<double[]> trainingData;

        private double a;
        private double b;
        private double c;

        public CalibrationFunction(List<double[]> trainingData, List<double> labelData)
        {
            this.trainingData = trainingData;
            this.labelData = labelData;

            int m = trainingData.Count;
            int n = trainingData[0].Length;

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            var X = M.DenseOfRowArrays(trainingData);
            var y = V.DenseOfEnumerable(labelData);
            
            var coeffs = X.Solve(y);

            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];

        }

        internal double calibrate(double mz, double retentionTime)
        {
            //return 0.01;

            return a + b * mz + c * retentionTime;

            // Quadratic model?
            //return -1.023e-03 - 7.605e-08 * retentionTime - 8.595e-07 * mz + 4.056e-08 * (retentionTime * retentionTime) - 3.030e-09 * (mz *mz) + 2.911e-08 * (retentionTime * mz);


        }
    }
}