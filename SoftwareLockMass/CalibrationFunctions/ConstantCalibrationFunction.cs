using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class ConstantCalibrationFunction : CalibrationFunction
    {
        private double a;

        public override double Predict(DataPoint t)
        {
            return a;
        }

        public override void Train(List<TrainingPoint> trainingList)
        {
            a = trainingList.Select(b => b.l).Average();
        }
    }
}