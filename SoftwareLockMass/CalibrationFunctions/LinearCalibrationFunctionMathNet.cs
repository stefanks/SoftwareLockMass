using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoftwareLockMass
{
    public class LinearCalibrationFunctionMathNet : CalibrationFunction
    {
        Func<double[], double> f;
        private Action<OutputHandlerEventArgs> onOutput;

        public LinearCalibrationFunctionMathNet(Action<OutputHandlerEventArgs> onOutput, IEnumerable<TrainingPoint> trainingList)
        {
            this.onOutput = onOutput;
            Train(trainingList);
        }

        public override double Predict(DataPoint t)
        {
            return f(new double[] { 1, t.mz, t.rt });
        }

        public void Train(IEnumerable<TrainingPoint> trainingList)
        {
            double[][] ok = new double[trainingList.Count()][];
            int k = 0;
            foreach (TrainingPoint p in trainingList)
            {
                ok[k] = new double[] { p.dp.mz, p.dp.rt };
                k++;
            }

            for (int i = 0; i < ok.Count(); i++)
                for (int j = 0; j < ok[0].Count(); j++)
                    Console.WriteLine(ok[i][j]);

            Console.WriteLine(ok);
            Console.WriteLine(ok.Count());
            Console.WriteLine(ok[0].Count());
            var ok2 = trainingList.Select(b => b.l).ToArray();
            Console.WriteLine(ok2);
            Console.WriteLine(ok2.Count());

            Func<double[], double> myFunc1 = (a) => 1;
            Func<double[], double> myFunc2 = (a) => a[0];
            Func<double[], double> myFunc3 = (a) => a[1];
            Console.WriteLine("Trying default");
            f = Fit.LinearMultiDimFunc(ok, ok2, new Func<double[], double>[] { myFunc1, myFunc2, myFunc3 });
            Console.WriteLine(f(new double[] { 3, 4 }));
            Console.WriteLine("Trying svd");
            f = Fit.LinearMultiDimFunc(ok, ok2, MathNet.Numerics.LinearRegression.DirectRegressionMethod.Svd, new Func<double[], double>[] { myFunc1, myFunc2, myFunc3 });
            Console.WriteLine(f(new double[] { 3, 4 }));
            Console.WriteLine("Trying qr");
            f = Fit.LinearMultiDimFunc(ok, ok2, MathNet.Numerics.LinearRegression.DirectRegressionMethod.QR, new Func<double[], double>[] { myFunc1, myFunc2, myFunc3 });
            Console.WriteLine(f(new double[] { 3, 4 }));
            Console.WriteLine("Trying normal");
            f = Fit.LinearMultiDimFunc(ok, ok2, MathNet.Numerics.LinearRegression.DirectRegressionMethod.NormalEquations, new Func<double[], double>[] { myFunc1, myFunc2, myFunc3 });
            Console.WriteLine(f(new double[] { 3, 4 }));

            Console.WriteLine("Trying default");
            f = Fit.MultiDimFunc(ok, ok2, true);
            Console.WriteLine(f(new double[] { 1, 3, 4 }));
            Console.WriteLine("Trying svd");
            f = Fit.MultiDimFunc(ok, ok2, true, MathNet.Numerics.LinearRegression.DirectRegressionMethod.Svd);
            Console.WriteLine(f(new double[] { 1, 3, 4 }));
            Console.WriteLine("Trying qr");
            f = Fit.MultiDimFunc(ok, ok2, true, MathNet.Numerics.LinearRegression.DirectRegressionMethod.QR);
            Console.WriteLine(f(new double[] { 1, 3, 4 }));
            Console.WriteLine("Trying normal");
            f = Fit.MultiDimFunc(ok, ok2, true, MathNet.Numerics.LinearRegression.DirectRegressionMethod.NormalEquations);
            Console.WriteLine(f(new double[] { 1, 3, 4 }));


        }
    }
}