using System;

namespace SoftwareLockMass
{
    internal class CalibratedSpectrum
    {
        public double[] mzValues;
        internal void AddMZValues(double[] mzValues)
        {
            this.mzValues = mzValues;
        }
    }
}