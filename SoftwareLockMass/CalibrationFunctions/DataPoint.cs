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

        internal double[] ToDoubleArrayWithInterceptAndSquares()
        {
            return new double[6] { 1, mz, rt, Math.Pow(mz, 2), Math.Pow(rt, 2), mz * rt };
        }

        internal double[] ToDoubleArrayWithInterceptAndSquaresAndCubes()
        {
            return new double[10] { 1, mz, rt, Math.Pow(mz, 2), Math.Pow(rt, 2), mz * rt, Math.Pow(mz, 3), Math.Pow(mz, 2) * rt, mz * Math.Pow(rt, 2), Math.Pow(rt, 3) };
        }

        internal double[] ToDoubleArrayWithInterceptAndSquaresAndCubesAndQuarts()
        {
            return new double[15] { 1, mz, rt, Math.Pow(mz, 2), Math.Pow(rt, 2), mz * rt, Math.Pow(mz, 3), Math.Pow(mz, 2) * rt, mz * Math.Pow(rt, 2), Math.Pow(rt, 3), Math.Pow(mz, 4) , Math.Pow(mz, 3) * rt  , Math.Pow(mz, 2) * Math.Pow(rt, 2) , mz * Math.Pow(rt, 3) , Math.Pow(rt, 4) };
        }
    }
}