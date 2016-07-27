﻿using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    public class IdentityCalibrationFunction : CalibrationFunction
    {
        private Action<OutputHandlerEventArgs> onOutput;

        public IdentityCalibrationFunction(Action<OutputHandlerEventArgs> onOutput)
        {
            this.onOutput = onOutput;
        }

        public override double Predict(double[] inputs)
        {
            return 0;
        }

        public void Train(List<TrainingPoint> trainingList)
        {
            onOutput(new OutputHandlerEventArgs("Sucessfully trained IdentityCalibrationFunction"));
        }
    }
}