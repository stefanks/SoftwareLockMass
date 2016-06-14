using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class IdentityCalibrationFunction : CalibrationFunction
    {
        private Action<OutputHandlerEventArgs> onOutput;

        public IdentityCalibrationFunction(Action<OutputHandlerEventArgs> onOutput)
        {
            this.onOutput = onOutput;
        }

        public override double Predict(DataPoint t)
        {
            return 0;
        }

        public override void Train(List<TrainingPoint> trainingList)
        {
            onOutput(new OutputHandlerEventArgs("Sucessfully trained IdentityCalibrationFunction"));
        }
    }
}