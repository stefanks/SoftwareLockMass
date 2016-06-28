using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class ConstantCalibrationFunction : CalibrationFunction
    {
        private double a;
        private Action<OutputHandlerEventArgs> onOutput;

        public ConstantCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, List<TrainingPoint> trainingList)
        {
            this.onOutput = onOutput;
            Train(trainingList);
        }

        public override double Predict(DataPoint t)
        {
            return a;
        }

        public void Train(List<TrainingPoint> trainingList)
        {
            a = trainingList.Select(b => b.l).Average();
            onOutput(new OutputHandlerEventArgs("Sucessfully trained ConstantCalibrationFunction"));
            onOutput(new OutputHandlerEventArgs("a = "+ a));
        }
    }
}