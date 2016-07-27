using System;

namespace SoftwareLockMass
{
    public class DataPoint
    {
        public double mz;
        public double rt;
        public int msnOrder;
        public double intensity;
        public int SelectedIonGuessChargeStateGuess;
        public double IsolationMZ;
        public double TotalIonCurrent;
        public double InjectionTime;
        public double relativeMZ;

        public DataPoint(double mz, double rt, int msnOrder, double intensity, double TotalIonCurrent, double InjectionTime, int SelectedIonGuessChargeStateGuess = 0, double IsolationMZ = 0, double relativeMZ = 0)
        {
            this.mz = mz;
            this.rt = rt;
            this.msnOrder = msnOrder;
            this.intensity = intensity;
            this.SelectedIonGuessChargeStateGuess = SelectedIonGuessChargeStateGuess;
            this.IsolationMZ = IsolationMZ;
            this.TotalIonCurrent= TotalIonCurrent;
            this.InjectionTime= InjectionTime;
            this.relativeMZ= relativeMZ;
    }

        internal double[] ToDoubleArrayWithIntercept()
        {
            return new double[3] { 1, mz, rt };
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
            return new double[15] { 1, mz, rt, Math.Pow(mz, 2), Math.Pow(rt, 2), mz * rt, Math.Pow(mz, 3), Math.Pow(mz, 2) * rt, mz * Math.Pow(rt, 2), Math.Pow(rt, 3), Math.Pow(mz, 4), Math.Pow(mz, 3) * rt, Math.Pow(mz, 2) * Math.Pow(rt, 2), mz * Math.Pow(rt, 3), Math.Pow(rt, 4) };
        }
        public override string ToString()
        {
            return "(" + mz + "," + rt + ")";
        }

        internal double[] ToDoubleArrayWithInterceptAndSquaresAndCubesAndQuartsAndFifths()
        {
            return new double[21] { 1, mz, rt, Math.Pow(mz, 2), Math.Pow(rt, 2), mz * rt, Math.Pow(mz, 3), Math.Pow(mz, 2) * rt, mz * Math.Pow(rt, 2), Math.Pow(rt, 3), Math.Pow(mz, 4), Math.Pow(mz, 3) * rt, Math.Pow(mz, 2) * Math.Pow(rt, 2), mz * Math.Pow(rt, 3), Math.Pow(rt, 4), Math.Pow(mz, 5), Math.Pow(mz, 4) * rt, Math.Pow(mz, 3) * Math.Pow(rt, 2), Math.Pow(mz, 2) * Math.Pow(rt, 3), mz * Math.Pow(rt, 4), Math.Pow(rt, 5) };
        }

        internal double[] ToDoubleArrayWithInterceptAndSquaresAndCubesAndQuartsAndFifthsAndSixths()
        {
            return new double[28] { 1, mz, rt, Math.Pow(mz, 2), Math.Pow(rt, 2), mz * rt, Math.Pow(mz, 3), Math.Pow(mz, 2) * rt, mz * Math.Pow(rt, 2), Math.Pow(rt, 3), Math.Pow(mz, 4), Math.Pow(mz, 3) * rt, Math.Pow(mz, 2) * Math.Pow(rt, 2), mz * Math.Pow(rt, 3), Math.Pow(rt, 4), Math.Pow(mz, 5), Math.Pow(mz, 4) * rt, Math.Pow(mz, 3) * Math.Pow(rt, 2), Math.Pow(mz, 2) * Math.Pow(rt, 3), mz * Math.Pow(rt, 4), Math.Pow(rt, 5), Math.Pow(mz, 6), Math.Pow(mz, 5) * rt, Math.Pow(mz, 4) * Math.Pow(rt, 2), Math.Pow(mz, 3) * Math.Pow(rt, 3), Math.Pow(mz, 2) * Math.Pow(rt, 4), mz * Math.Pow(rt, 5), Math.Pow(rt, 6) };
        }
    }
}