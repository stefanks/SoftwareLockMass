using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using SoftwareLockMass.CalibrationFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class KDTreeCalibrationFunction : CalibrationFunction
    {
        KDTree kdTree;
        private Action<OutputHandlerEventArgs> onOutput;

        public KDTreeCalibrationFunction()
        {
        }

        public KDTreeCalibrationFunction(Action<OutputHandlerEventArgs> onOutput)
        {
            this.onOutput = onOutput;
        }

        public override void Train(List<TrainingPoint> trainingList)
        {
            kdTree = new KDTree(onOutput, trainingList);
            onOutput(new OutputHandlerEventArgs("Finished Generating Tree"));
        }
        
        public override double Predict(DataPoint t)
        {
            return kdTree.GetFinestContainingTree(t).GetFirstLabel();
        }

    }
}