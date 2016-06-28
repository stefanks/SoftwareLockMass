using System;
using System.Collections.Generic;

namespace SoftwareLockMass
{
    public abstract class CalibrationFunction
    {
        public abstract double Predict(DataPoint t);
        public double getMSE(IEnumerable<TrainingPoint> pointList)
        {
            double mse = 0;
            int count = 0;
            foreach(TrainingPoint p in pointList)
            {
                mse += Math.Pow(Predict(p.dp) - p.l, 2);
                count++;
            }
            return mse / count;
        }
    }
}