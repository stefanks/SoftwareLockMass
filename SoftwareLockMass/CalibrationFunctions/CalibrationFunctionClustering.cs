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
        private Action<OutputHandlerEventArgs> onOutput;
        private int v;

        private double dist(double v1, double v2, int j, double[] CentroidMZdist, double[] CentroidTimedist)
        {
            return Math.Pow(v1 - CentroidMZdist[j],2) + Math.Pow(v2 - CentroidTimedist[j],2);
        }
        

        public CalibrationFunctionClustering(Action<OutputHandlerEventArgs> onOutput, int numClusters)
        {
            this.onOutput = onOutput;
            this.numClusters = numClusters;
        }

        public override void Train(List<TrainingPoint> trainingList)
        {
            int numTp = trainingList.Count;
            Random randNum = new Random();
            double totalError = double.MaxValue;

            for (int numTries = 0; numTries < 3000; numTries++)
            {
                bool changeHappend = true;
                var Cluster = Enumerable.Repeat(0, numTp).ToArray();
                double[] errorAtCluster = new double[numClusters];
                double[] CentroidMZ = new double[numClusters];
                double[] CentroidTime = new double[numClusters];
                int[] PointsInCluster;
                double[] mseInCluster = Enumerable.Repeat(0.0, numClusters).ToArray();

                // Guess initial centroids
                for (int i = 0; i < numClusters; i++)
                {
                    var kk = randNum.Next(numTp);
                    //p.OnOutput(new OutputHandlerEventArgs((kk);
                    CentroidMZ[i] = trainingList[kk].dp.mz;
                    CentroidTime[i] = trainingList[kk].dp.rt;
                    errorAtCluster[i] = trainingList[kk].l;
                }


                while (changeHappend)
                {
                    changeHappend = false;

                    // Assign every point to cluster
                    for (int i = 0; i < numTp; i++)
                    {
                        double bestDistance = double.MaxValue;
                        var oldClusterI = Cluster[i];
                        for (int j = 0; j < numClusters; j++)
                        {
                            if (dist(trainingList[i].dp.mz, trainingList[i].dp.rt, j, CentroidMZ, CentroidTime)  < bestDistance)
                            {
                                bestDistance = dist(trainingList[i].dp.mz, trainingList[i].dp.rt, j, CentroidMZ, CentroidTime);
                                Cluster[i] = j;
                            }
                        }
                        if (Cluster[i] != oldClusterI)
                            changeHappend = true;
                    }

                    // Recalculate cluster centroids
                    CentroidMZ = Enumerable.Repeat(0.0, numClusters).ToArray();
                    CentroidTime = Enumerable.Repeat(0.0, numClusters).ToArray();
                    errorAtCluster = Enumerable.Repeat(0.0, numClusters).ToArray();
                    PointsInCluster = Enumerable.Repeat(0, numClusters).ToArray();
                    for (int i = 0; i < numTp; i++)
                    {
                        CentroidMZ[Cluster[i]] += trainingList[i].dp.mz;
                        CentroidTime[Cluster[i]] += trainingList[i].dp.rt;
                        errorAtCluster[Cluster[i]] += trainingList[i].l;
                        PointsInCluster[Cluster[i]] += 1;
                    }
                    for (int j = 0; j < numClusters; j++)
                    {
                        CentroidMZ[j] = CentroidMZ[j] / PointsInCluster[j];
                        CentroidTime[j] = CentroidTime[j] / PointsInCluster[j];
                        errorAtCluster[j] = errorAtCluster[j] / PointsInCluster[j];
                    }
                }
                
                for (int i = 0; i < numTp; i++)
                {
                    mseInCluster[Cluster[i]] += Math.Pow(trainingList[i].l - errorAtCluster[Cluster[i]],2);
                }
                if (totalError > mseInCluster.Sum())
                {
                    CentroidMZfinal = (double[])CentroidMZ.Clone();
                    CentroidTimefinal = (double[])CentroidTime.Clone();
                    errorAtClusterfinal = (double[])errorAtCluster.Clone();

                    totalError = mseInCluster.Sum();
                    onOutput(new OutputHandlerEventArgs("New total MSE: " + totalError));
                    onOutput(new OutputHandlerEventArgs("New total MSE other calucation: " + getMSE(trainingList)));
                    for (int j = 0; j < numClusters; j++)
                    {
                        onOutput(new OutputHandlerEventArgs(" " + CentroidMZ[j] + " " + CentroidTime[j] + " " + errorAtCluster[j]));
                    }
                }
            }
        }

        public override double Predict(DataPoint t)
        {
            int theCluster = -1;
            double bestDistance = double.MaxValue;
            for (int j = 0; j < numClusters; j++)
            {
                if (dist(t.mz, t.rt, j, CentroidMZfinal, CentroidTimefinal) < bestDistance)
                {
                    bestDistance = dist(t.mz, t.rt, j, CentroidMZfinal, CentroidTimefinal);
                    theCluster = j;
                }
            }
            return errorAtClusterfinal[theCluster];
        }
    }
}