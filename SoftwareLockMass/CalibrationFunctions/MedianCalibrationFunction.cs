using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class MedianCalibrationFunction : CalibrationFunction
    {
        double[,] theErrors;

        int mzCount;
        int rtCount;
        double maxMZ;
        double maxrt;
        bool average;
        int numToSelect;
        double relativeWeight;
        private Action<OutputHandlerEventArgs> onOutput;

        public MedianCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, int mzCount= 50, int rtCount = 50, bool average = false, int numToSelect = 10, double maxMZ = 2000, double maxrt = 200, double relativeWeight = 1)
        {
            this.mzCount = mzCount;
            this.rtCount = rtCount;
            this.maxMZ = maxMZ;
            this.maxrt = maxrt;
            this.average = average;
            this.numToSelect= numToSelect;
            this.relativeWeight = relativeWeight;
            this.onOutput = onOutput;
        }
        
        public override void Train(List<TrainingPoint> trainingList)
        {
            theErrors = new double[mzCount, rtCount];
            
            for (int i = 0; i < mzCount; i++)
            {
                for (int j = 0; j < rtCount; j++)
                {
                    trainingList.Sort(delegate (TrainingPoint t1, TrainingPoint t2)
                    { return dist(indexToMz(i), indexToRt(j), t1.dp).CompareTo(dist(indexToMz(i), indexToRt(j), t2.dp)); });
                    if (average)
                        theErrors[i, j] = trainingList.Take(numToSelect).Select(b => b.l).Average();
                    else
                        theErrors[i, j] = trainingList.Take(numToSelect).Select(b => b.l).Median();
                }
            }

        }

        // Gives center of interval
        private double indexToRt(int j)
        {
            return maxrt / (2 * rtCount) * (2* j + 1);
        }
        
        // Gives center of interval
        private double indexToMz(int i)
        {
            return maxMZ / (2*mzCount) * (2 * i + 1);
        }

        private int rtToIndex(double rt)
        {
            return Convert.ToInt32((rt * 2 * rtCount / maxrt - 1) / 2);
        }

        private int mzToIndex(double mz)
        {
            return Convert.ToInt32((mz * 2 * mzCount / maxMZ - 1) / 2);
        }

        private double dist(double mz, double rt, DataPoint dp)
        {
            return Math.Pow(dp.mz - mz, 2) +relativeWeight * Math.Pow(dp.rt - rt, 2);
        }

        public override double Predict(DataPoint t)
        {
            return theErrors[mzToIndex(t.mz), rtToIndex(t.rt)];
        }

    }
}