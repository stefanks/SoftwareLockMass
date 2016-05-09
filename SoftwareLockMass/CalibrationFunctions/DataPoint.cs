using System;

namespace SoftwareLockMass
{
    public class DataPoint
    {
        public double mz;
        public double rt;
        public DataPoint(double _mz, double _rt)
        {
            mz = _mz;
            rt = _rt;
        }

        internal double[] ToDoubleArrayWithIntercept()
        {
            return  new double[3] { 1, mz , rt};
        }
    }
}