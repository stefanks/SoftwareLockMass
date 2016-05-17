using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareLockMass.CalibrationFunctions
{
    class KDTree
    {
        private int depth;
        private List<TrainingPoint> trainingList;
        private double splitValue;
        private KDTree leftChild = null;
        private KDTree rightChild = null;
        private Func<DataPoint, double> separatingFunction;
        private Func<DataPoint, bool> functionToSeparateLeft;
        private Func<DataPoint, bool> functionToSeparateRight;

        public KDTree(List<TrainingPoint> trainingList, int depth = 0, bool parentHadOneChild = false)
        {
            this.trainingList = trainingList;
            this.depth = depth;

            if (depth % 2 == 0)
                separatingFunction = b => b.mz;
            else
                separatingFunction = b => b.rt;
            var myListOfValues = trainingList.Select(c => separatingFunction(c.dp));
            splitValue = myListOfValues.Median();
            if ((myListOfValues.Max() == splitValue) != (myListOfValues.Min() == splitValue))
                splitValue = trainingList.Select(c => separatingFunction(c.dp)).Distinct().Median();

            functionToSeparateLeft = b => separatingFunction(b) < splitValue;
            functionToSeparateRight = b => separatingFunction(b) >= splitValue;
            
            List<TrainingPoint> left = trainingList.Where(c => functionToSeparateLeft(c.dp)).ToList();
            List<TrainingPoint> right = trainingList.Where(c => functionToSeparateRight(c.dp)).ToList();

            if ((left.Count() > 0 && right.Count() > 0) || parentHadOneChild == false)
            {
                if (left.Count() > 0)
                    leftChild = new KDTree(left, depth + 1, right.Count() == 0);
                if (right.Count() > 0)
                    rightChild = new KDTree(right, depth + 1, left.Count() == 0);
            }

            if (depth  <5)
                Console.WriteLine("depth = " + depth + " left.Count()  " + left.Count() + " right.Count() " + right.Count() + " median " + splitValue);
        }

        internal double GetFirstLabel()
        {
            return trainingList.First().l;
        }

        public KDTree GetFinestContainingTree(DataPoint arg)
        {
            if (separatingFunction(arg) < splitValue)
            {
                if (leftChild != null)
                    return leftChild.GetFinestContainingTree(arg);
                else
                    return this;
            }
            else
            {
                if (rightChild != null)
                    return rightChild.GetFinestContainingTree(arg);
                else
                    return this;
            }
        }
    }
}
