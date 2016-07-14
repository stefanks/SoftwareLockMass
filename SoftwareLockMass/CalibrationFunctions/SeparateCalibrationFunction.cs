using System;

namespace SoftwareLockMass
{
    internal class SeparateCalibrationFunction : CalibrationFunction
    {
        private CalibrationFunction calibrationFunction1;
        private CalibrationFunction calibrationFunction2;

        public SeparateCalibrationFunction(CalibrationFunction calibrationFunction1, CalibrationFunction calibrationFunction2)
        {
            this.calibrationFunction1 = calibrationFunction1;
            this.calibrationFunction2 = calibrationFunction2;
        }

        public override double Predict(DataPoint t)
        {
            if (t.msnOrder == 1)
                return calibrationFunction1.Predict(t);
            else
                return calibrationFunction2.Predict(t);
        }
    }
}