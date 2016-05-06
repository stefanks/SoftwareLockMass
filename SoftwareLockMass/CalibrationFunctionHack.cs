using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace SoftwareLockMass
{
    internal class CalibrationFunctionHack
    {
        private double a;
        private double b;
        private double c;

        private double d;
        private double e;
        private double f;


        public CalibrationFunctionHack(List<double[]> trainingData, List<double> labelData)
        {
            List<double[]> trainingDataLow = new List<double[]>();
            List<double[]> trainingDataHigh = new List<double[]>();
            List<double> labelDataLow = new List<double>();
            List<double> labelDataHigh = new List<double>();


            for (int i = 0; i < labelData.Count; i++)
            {
                if (trainingData[i][2] <120)
                {
                    trainingDataLow.Add(trainingData[i]);
                    labelDataLow.Add(labelData[i]);
                }
                else
                {
                    trainingDataHigh.Add(trainingData[i]);
                    labelDataHigh.Add(labelData[i]);
                }
            }

            
            
            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            var X = M.DenseOfRowArrays(trainingDataLow);
            var y = V.DenseOfEnumerable(labelDataLow);

            var coeffs = X.Solve(y);

            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];

             X = M.DenseOfRowArrays(trainingDataHigh);
             y = V.DenseOfEnumerable(labelDataHigh);

             coeffs = X.Solve(y);

            d = coeffs[0];
            e = coeffs[1];
            f = coeffs[2];

        }

        internal double calibrate(double mz, double retentionTime)
        {
            if (retentionTime < 120)
                return a + b * mz + c * retentionTime;
            else
                return d + e * mz + f * retentionTime; 
        }
    }
}