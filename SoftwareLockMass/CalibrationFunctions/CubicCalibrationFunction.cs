using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    internal class CubicCalibrationFunction : CalibrationFunction
    {
        private double a;
        private double b;
        private double c;

        private double d;
        private double e;
        private double f;

        private double g;
        private double h;
        private double i;
        private double j;
        private Action<OutputHandlerEventArgs> onOutput;

        public CubicCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, List<TrainingPoint> trainingList)
        {
            this.onOutput = onOutput;
            Train(trainingList);
        }

        public override double Predict(DataPoint t)
        {
            return a + b * t.mz + c * t.rt + d*Math.Pow(t.mz,2) +e * Math.Pow(t.rt, 2) + f * t.mz*t.rt +
                g * Math.Pow(t.mz, 3)+
                h * Math.Pow(t.mz, 2)* t.rt + 
                i * t.mz*Math.Pow(t.rt, 2) + 
                j * Math.Pow(t.rt, 3);
        }

        public void Train(List<TrainingPoint> trainingList)
        {

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;
            
            var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithInterceptAndSquaresAndCubes()));
            var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

            var coeffs = X.Solve(y);
            if (double.IsNaN(coeffs[0]))
                throw new ArgumentException("Could not train CubicCalibrationFunction, data might be low rank");


            a = coeffs[0];
            b = coeffs[1];
            c = coeffs[2];
            d = coeffs[3];
            e = coeffs[4];
            f = coeffs[5];
            g = coeffs[6];
            h = coeffs[7];
            i = coeffs[8];
            j = coeffs[9];

            onOutput(new OutputHandlerEventArgs("Sucessfully trained CubicCalibrationFunction"));
        }
    }
}