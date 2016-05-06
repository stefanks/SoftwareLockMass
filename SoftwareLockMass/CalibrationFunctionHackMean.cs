using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;

namespace SoftwareLockMass
{
    internal class CalibrationFunctionHackMean
    {
        double totalErrorA;
        double totalErrorB;
        double totalErrorC;
        double totalErrorD;
        int countA;
        int countB;
        int countC;
        int countD;


        public CalibrationFunctionHackMean(List<double[]> trainingData, List<double> labelData)
        {
            totalErrorA = 0;
            totalErrorB = 0;
            totalErrorC = 0;
            totalErrorD = 0;
            countA = 0;
            countB = 0;
            countC = 0;
            countD = 0;

            for (int i = 0; i < labelData.Count; i++)
            {
                if (trainingData[i][2] < 40 && trainingData[i][1] < 600)
                {
                    totalErrorA += labelData[i];
                    countA += 1;
                }
                else if (trainingData[i][2] < 40 && trainingData[i][1] >= 600)
                {
                    totalErrorB += labelData[i];
                    countB += 1;
                }
                else if (trainingData[i][2] >= 40 && trainingData[i][2] < 120)
                {
                    totalErrorC += labelData[i];
                    countC += 1;
                }
                else if (trainingData[i][2] >= 120)
                {
                    totalErrorD += labelData[i];
                    countD += 1;
                }
            }


        }

        internal double calibrate(double mz, double retentionTime)
        {

            if (retentionTime < 40 && mz < 600)
            {
                return totalErrorA / countA;
            }
            else if (retentionTime < 40 && mz >= 600)
            {
                return totalErrorB / countB;
            }
            else if (retentionTime >= 40 && retentionTime < 120)
            {
                return totalErrorC / countC;
            }
            else if (retentionTime >= 120)
            {
                return totalErrorD / countD;
            }
            else
                throw new Exception();
        }
    }
}