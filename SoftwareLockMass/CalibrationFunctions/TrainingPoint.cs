﻿namespace SoftwareLockMass
{
    public class TrainingPoint
    {
        public DataPoint dp;
        public double l;
        public TrainingPoint(DataPoint t, double label)
        {
            dp = t;
            l = label;
        }
    }
}