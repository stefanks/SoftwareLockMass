using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mzCal
{
    public class DecisionTreeRegressor : CalibrationFunction
    {
        RegressionTree ok;
        bool[] useFeature;
        int numFeatures;

        public DecisionTreeRegressor(List<LabeledDataPoint> trainingList, bool[] useFeature)
        {
            this.useFeature = useFeature;

            numFeatures = useFeature.Where(b => b == true).Count();

            ok = new RegressionTree();

            List<LabeledDataPoint> subsampledTrainingPoints = new List<LabeledDataPoint>();
            for (int j = 0; j < trainingList.Count; j++)
            {
                var yeesh = new LabeledDataPoint(IndexMap(trainingList[j].inputs), trainingList[j].output);

                subsampledTrainingPoints.Add(yeesh);



            }
            ok.Train(subsampledTrainingPoints, 1, 0);
        }

        public override double Predict(double[] input)
        {
            return ok.predict(IndexMap(input));
        }

        private double[] IndexMap(double[] input)
        {
            double[] output = new double[numFeatures];
            int featInd = 0;
            for (int k = 0; k < useFeature.Length; k++)
                if (useFeature[k])
                {
                    output[featInd] = input[k];
                    featInd++;
                }
            return output;
        }
    }
}