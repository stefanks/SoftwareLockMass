//using MathNet.Numerics.LinearAlgebra;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace SoftwareLockMass
//{
//    internal class SixthCalibrationFunction : CalibrationFunction
//    {
//        private double a;
//        private double b;
//        private double c;

//        private double d;
//        private double e;
//        private double f;

//        private double g;
//        private double h;
//        private double i;
//        private double j;

//        private double k;
//        private double l;
//        private double m;
//        private double n;
//        private double o;

//        private double p;
//        private double q;
//        private double r;
//        private double s;
//        private double t;
//        private double u;

//        private double s1;
//        private double s2;
//        private double s3;
//        private double s4;
//        private double s5;
//        private double s6;
//        private double s7;




//        private Action<OutputHandlerEventArgs> onOutput;

//        public SixthCalibrationFunction(Action<OutputHandlerEventArgs> onOutput, IEnumerable<TrainingPoint> trainingList)
//        {
//            this.onOutput = onOutput;
//            Train(trainingList);
//        }

//        public override double Predict(DataPoint dp)
//        {
//            return a + b * dp.mz + c * dp.rt + d * Math.Pow(dp.mz, 2) + e * Math.Pow(dp.rt, 2) + f * dp.mz * dp.rt +
//                g * Math.Pow(dp.mz, 3) +
//                h * Math.Pow(dp.mz, 2) * dp.rt +
//                i * dp.mz * Math.Pow(dp.rt, 2) +
//                j * Math.Pow(dp.rt, 3) +
//                k * Math.Pow(dp.mz, 4) +
//                l * Math.Pow(dp.mz, 3) * dp.rt +
//                m * Math.Pow(dp.mz, 2) * Math.Pow(dp.rt, 2) +
//                n * dp.mz * Math.Pow(dp.rt, 3) +
//                o * Math.Pow(dp.rt, 4) +
//                p * Math.Pow(dp.mz, 5) +
//                q * Math.Pow(dp.mz, 4) * dp.rt +
//                r * Math.Pow(dp.mz, 3) * Math.Pow(dp.rt, 2) +
//                s * Math.Pow(dp.mz, 2) * Math.Pow(dp.rt, 3) +
//                t * dp.mz * Math.Pow(dp.rt, 4) +
//                u * Math.Pow(dp.rt, 5) +
//                s1 * Math.Pow(dp.mz, 6) +
//                s2 * Math.Pow(dp.mz, 5) * dp.rt +
//                s3 * Math.Pow(dp.mz, 4) * Math.Pow(dp.rt, 2) +
//                s4 * Math.Pow(dp.mz, 3) * Math.Pow(dp.rt, 3) +
//                s5 * Math.Pow(dp.mz, 2) * Math.Pow(dp.rt, 4) +
//                s6 * dp.mz * Math.Pow(dp.rt, 5) +
//                s7 * Math.Pow(dp.rt, 6);


//        }

//        public void Train(IEnumerable<TrainingPoint> trainingList)
//        {

//            var M = Matrix<double>.Build;
//            var V = Vector<double>.Build;

//            var X = M.DenseOfRowArrays(trainingList.Select(b => b.dp.ToDoubleArrayWithInterceptAndSquaresAndCubesAndQuartsAndFifthsAndSixths()));
//            var y = V.DenseOfEnumerable(trainingList.Select(b => b.l));

//            var coeffs = X.Solve(y);
//            if (double.IsNaN(coeffs[0]))
//                throw new ArgumentException("Could not train SixthCalibrationFunction, data might be low rank");

//            a = coeffs[0];
//            b = coeffs[1];
//            c = coeffs[2];
//            d = coeffs[3];
//            e = coeffs[4];
//            f = coeffs[5];
//            g = coeffs[6];
//            h = coeffs[7];
//            i = coeffs[8];
//            j = coeffs[9];
//            k = coeffs[10];
//            l = coeffs[11];
//            m = coeffs[12];
//            n = coeffs[13];
//            o = coeffs[14];
//            p = coeffs[15];
//            q = coeffs[16];
//            r = coeffs[17];
//            s = coeffs[18];
//            t = coeffs[19];
//            u = coeffs[20];

//            s1 = coeffs[21];
//            s2 = coeffs[22];
//            s3 = coeffs[23];
//            s4 = coeffs[24];
//            s5 = coeffs[25];
//            s6 = coeffs[26];
//            s7 = coeffs[27];


//            onOutput(new OutputHandlerEventArgs("Sucessfully trained SixthCalibrationFunction"));
//        }
//    }
//}