using System;
using System.Collections.Generic;

namespace SoftwareLockMass
{
    abstract class CalibrationFunction
    {
        public abstract void Train(List<TrainingPoint> trainingList);
        public abstract double Predict(DataPoint t);
        public double getMSE(List<TrainingPoint> trainingList)
        {
            double mse = 0;
            for (int i = 0; i < trainingList.Count; i++)
            {
                mse += Math.Pow(Predict(trainingList[i].dp) - trainingList[i].l, 2);
            }
            return mse;
        }
    }
}