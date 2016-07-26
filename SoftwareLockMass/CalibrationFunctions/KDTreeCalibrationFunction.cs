//using MathNet.Numerics.LinearAlgebra;
//using MathNet.Numerics.Statistics;
//using SoftwareLockMass.CalibrationFunctions;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace SoftwareLockMass
//{
//    internal class KDTreeCalibrationFunction : CalibrationFunction
//    {
//        KDTree kdTree;

//        public KDTreeCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, IEnumerable<TrainingPoint> trainingList)
//        {
//            kdTree = new KDTree(onOutput, trainingList);
//        }

//        public override double Predict(DataPoint t)
//        {
//            return kdTree.GetFinestContainingTree(t).GetFirstLabel();
//        }

//    }
//}