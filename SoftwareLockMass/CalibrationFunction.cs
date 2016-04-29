using System;
using System.Collections.Generic;

namespace SoftwareLockMass
{
    internal class CalibrationFunction
    {
        private List<double> labelData;
        private List<double[]> trainingData;

        public CalibrationFunction(List<double[]> trainingData, List<double> labelData)
        {
            this.trainingData = trainingData;
            this.labelData = labelData;
        }

        internal double calibrate(double mz, double retentionTime)
        {
            return 1;
        }
    }
}