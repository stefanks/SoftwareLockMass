using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class CalibrationFunctionClustering
    {
        private int numClusters;
        private double[] CentroidMZ;
        private double[] CentroidTime;
        private double[] errorAtCluster;

        public CalibrationFunctionClustering(List<double[]> trainingData, List<double> labelData, int numClusters = 5)
        {
            this.numClusters = numClusters;
            int numTp = labelData.Count;
            bool changeHappend = true;
            var Cluster = Enumerable.Repeat(0, numTp).ToArray();
            errorAtCluster = Enumerable.Repeat(0.0, numClusters).ToArray();

            CentroidMZ = new double[]
{
        400,
        400,
        900,
        1400,
        1400
};
            CentroidTime = new double[]
{
        30,
        170,
        100,
        170,
        30
};
            while (changeHappend)
            {
                changeHappend = false;
                int[] PointsInCluster = Enumerable.Repeat(0, numClusters).ToArray();

                for (int i = 0; i < numTp; i++)
                {
                    double bestDistance = double.MaxValue;
                     var oldClusterI = Cluster[i];
                    for (int j = 0; j < numClusters; j++)
                    {
                        if (dist(trainingData[i][1], trainingData[i][2], j) < bestDistance)
                        {
                            bestDistance = dist(trainingData[i][1], trainingData[i][2], j);
                            Cluster[i] = j;
                        }
                    }
                    if (Cluster[i] != oldClusterI)
                        changeHappend = true;
                }
                for (int i = 0; i < numTp; i++)
                {
                    CentroidMZ[Cluster[i]] += trainingData[i][1];
                    CentroidTime[Cluster[i]] += trainingData[i][2];
                    errorAtCluster[Cluster[i]] += labelData[i];
                    PointsInCluster[Cluster[i]] += 1;
                }
                for (int j = 0; j < numClusters; j++)
                {
                    CentroidMZ[j] = CentroidMZ[j] / PointsInCluster[j];
                    CentroidTime[j] = CentroidTime[j] / PointsInCluster[j];
                    errorAtCluster[j] = errorAtCluster[j] / PointsInCluster[j];
                }

            }
        }

        private double dist(double v1, double v2, int j)
        {
            return (v1 - CentroidMZ[j]) * (v1 - CentroidMZ[j]) + (v2 - CentroidTime[j]) * (v2 - CentroidTime[j]);
        }

        internal double calibrate(double mz, double retentionTime)
        {
            int theCluster = -1;
            double bestDistance = double.MaxValue;
            for (int j = 0; j < numClusters; j++)
            {
                if (dist(mz, retentionTime, j) < bestDistance)
                {
                    bestDistance = dist(mz, retentionTime, j);
                    theCluster = j;
                }
            }
            return errorAtCluster[theCluster];
        }
    }
}