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

        public KDTreeCalibrationFunction()
        {
        }

        public override void Train(List<TrainingPoint> trainingList)
        {
            kdTree = new KDTree(trainingList);
            Console.WriteLine("Finished Generating Tree");
        }
        
        public override double Predict(DataPoint t)
        {
            return kdTree.GetFinestContainingTree(t).GetFirstLabel();
        }

    }
}