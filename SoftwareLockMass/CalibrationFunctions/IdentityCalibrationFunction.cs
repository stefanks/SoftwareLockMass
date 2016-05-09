using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class IdentityCalibrationFunction : CalibrationFunction
    {
        

        public override double Predict(DataPoint t)
        {
            return 0;
        }

        public override void Train(List<TrainingPoint> trainingList)
        {
        }
    }
}