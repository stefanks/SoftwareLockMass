using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class CalibrationFunctionClustering : CalibrationFunction
    {
        private int numClusters;
        private double[] CentroidMZfinal;
        private double[] CentroidTimefinal;
        private double[] errorAtClusterfinal;

        public CalibrationFunctionClustering(List<double[]> trainingData, List<double> labelData, int numClusters = 3)
        {
            this.numClusters = numClusters;
            int numTp = labelData.Count;
            Random randNum = new Random();
            double totalError = double.MaxValue;

            for (int numTries = 0; numTries < 100000; numTries++)
            {
                bool changeHappend = true;
                var Cluster = Enumerable.Repeat(0, numTp).ToArray();
                double[] errorAtCluster = Enumerable.Repeat(0.0, numClusters).ToArray();
                double[] CentroidMZ = new double[numClusters] ;
                double[] CentroidTime = new double[numClusters];
                int[] PointsInCluster = Enumerable.Repeat(0, numClusters).ToArray();
                double[] mseInCluster = Enumerable.Repeat(0.0, numClusters).ToArray();
                for (int i = 0; i < numClusters; i++)
                {
                    var kk = randNum.Next(numTp);
                    //Console.WriteLine(kk);
                    CentroidMZ[i] = trainingData[kk][1];
                    CentroidTime[i] = trainingData[kk][2];
                }
                while (changeHappend)
                {
                    changeHappend = false;

                    for (int i = 0; i < numTp; i++)
                    {
                        double bestDistance = double.MaxValue;
                        var oldClusterI = Cluster[i];
                        for (int j = 0; j < numClusters; j++)
                        {
                            if (dist(trainingData[i][1], trainingData[i][2], j, CentroidMZ, CentroidTime) < bestDistance)
                            {
                                bestDistance = dist(trainingData[i][1], trainingData[i][2], j, CentroidMZ, CentroidTime);
                                Cluster[i] = j;
                            }
                        }
                        if (Cluster[i] != oldClusterI)
                            changeHappend = true;
                    }
                 }

                // Settled in on class for each point by now
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
                for (int i = 0; i < numTp; i++)
                {
                    mseInCluster[Cluster[i]] += (labelData[i] - errorAtCluster[Cluster[i]]) * (labelData[i] - errorAtCluster[Cluster[i]]);
                }
                
                if (totalError > mseInCluster.Sum())
                {
                    totalError = mseInCluster.Sum();
                    Console.WriteLine("totalMSE" + totalError);
                    //for (int j = 0; j < numClusters; j++)
                    //{
                    //    Console.WriteLine(CentroidMZ[j] + " " + CentroidTime[j] + " " + errorAtCluster[j]);
                    //}
                    CentroidMZfinal = (double[]) CentroidMZ.Clone();
                    CentroidTimefinal=(double[])CentroidTime.Clone();
                    errorAtClusterfinal=(double[])errorAtCluster.Clone();
                }
            }
        }

        private double dist(double v1, double v2, int j, double[] CentroidMZdist, double[] CentroidTimedist)
        {
            return (v1 - CentroidMZdist[j]) * (v1 - CentroidMZdist[j]) + (v2 - CentroidTimedist[j]) * (v2 - CentroidTimedist[j]);
        }

        internal double calibrate(double mz, double retentionTime)
        {
            int theCluster = -1;
            double bestDistance = double.MaxValue;
            for (int j = 0; j < numClusters; j++)
            {
                if (dist(mz, retentionTime, j, CentroidMZfinal, CentroidTimefinal) < bestDistance)
                {
                    bestDistance = dist(mz, retentionTime, j, CentroidMZfinal, CentroidTimefinal);
                    theCluster = j;
                }
            }
            return errorAtClusterfinal[theCluster];
        }

        public override void Train(List<TrainingPoint> trainingList)
        {
            throw new NotImplementedException();
        }

        public override double Predict(DataPoint t)
        {
            throw new NotImplementedException();
        }
    }
}