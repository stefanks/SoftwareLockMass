using System;
using System.Collections.Generic;
using System.IO;

namespace SoftwareLockMass
{
    public abstract class CalibrationFunction
    {
        public abstract double Predict(double[] t);
        public double getMSE(IEnumerable<LabeledDataPoint> pointList)
        {
            double mse = 0;
            int count = 0;
            foreach(LabeledDataPoint p in pointList)
            {
                mse += Math.Pow(Predict(p.inputs) - p.output, 2);
                count++;
            }
            return mse / count;
        }

        internal void writeNewLabels(List<LabeledDataPoint> trainList1, string v)
        {
            using (StreamWriter file = new StreamWriter(v+"newLabels" + ".dat"))
            {
                file.WriteLine("NewLabel");
                foreach (LabeledDataPoint d in trainList1)
                    file.WriteLine(Predict(d.inputs));
            }
        }
    }
}