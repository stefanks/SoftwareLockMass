using Chemistry;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareLockMass
{
    class Calibrators
    {
        public static void CalibrateTSV(CalibrationFunction cf, SoftwareLockMassParams p)
        {
            using (StreamReader reader = new StreamReader(p.tsvFile))
            using (StreamWriter file = new StreamWriter(p.tsvFile + ".calibrated"))
            {
                string line;
                int lines = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (lines > 0)
                    {
                        // Do something with the line.
                        string[] parts = line.Split('\t');

                        // Needed for getting RT for precursor
                        int MS2spectrumNumber = Convert.ToInt32(parts[1]);
                        var precursorID = p.myMsDataFile.GetScan(MS2spectrumNumber).PrecursorID;
                        int MS1spectrumNumber = MS2spectrumNumber - 1;
                        while (!precursorID.Equals(p.myMsDataFile.GetScan(MS1spectrumNumber).id))
                            MS1spectrumNumber--;

                        double precursorRT = p.myMsDataFile.GetScan(MS1spectrumNumber).RetentionTime;

                        // To Calibrate
                        double precursorMZ = Convert.ToDouble(parts[5]);

                        // To Recompute
                        double precursorMass = Convert.ToDouble(parts[8]);
                        double precursorMassErrorDA = Convert.ToDouble(parts[18]);
                        double precursorMassErrorppm = Convert.ToDouble(parts[19]);

                        // Other needed info
                        int precursorCharge = Convert.ToInt32(parts[7]);
                        double theoreticalMass = Convert.ToDouble(parts[17]);


                        precursorMZ -= cf.Predict(new DataPoint(precursorMZ, precursorRT));
                        precursorMass = precursorMZ.ToMass(precursorCharge);
                        precursorMassErrorDA = precursorMass - theoreticalMass;
                        precursorMassErrorppm = precursorMassErrorDA / theoreticalMass * 1e6;

                        parts[5] = precursorMZ.ToString();
                        parts[8] = precursorMass.ToString();
                        parts[18] = precursorMassErrorDA.ToString();
                        parts[19] = precursorMassErrorppm.ToString();

                        file.WriteLine(string.Join("\t", parts));
                    }
                    else
                        file.WriteLine(line);
                    lines++;
                }
            }
        }

        public static List<IMzSpectrum<MzPeak>> CalibrateSpectra(CalibrationFunction cf, SoftwareLockMassParams p)
        {
            List<IMzSpectrum<MzPeak>> calibratedSpectra = new List<IMzSpectrum<MzPeak>>();
            for (int i = 0; i < p.myMsDataFile.LastSpectrumNumber; i++)
            {
                if (p.myMsDataFile.LastSpectrumNumber < 100)
                    p.OnProgress(new ProgressHandlerEventArgs(i));
                else if (i % (p.myMsDataFile.LastSpectrumNumber / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs((i / (p.myMsDataFile.LastSpectrumNumber / 100))));
                if (p.MS1spectraToWatch.Contains(i + 1))
                {
                    p.OnWatch(new OutputHandlerEventArgs("Before calibration of spectrum " + (i + 1)));
                    var mzs = p.myMsDataFile.GetSpectrum(i + 1).Extract(p.mzRange);
                    p.OnWatch(new OutputHandlerEventArgs(string.Join(", ", mzs)));
                }
                calibratedSpectra.Add(p.myMsDataFile.GetSpectrum(i + 1).CorrectMasses(s => s - cf.Predict(new DataPoint(s, p.myMsDataFile.GetScan(i + 1).RetentionTime))));
                if (p.MS1spectraToWatch.Contains(i + 1))
                {
                    p.OnWatch(new OutputHandlerEventArgs("After calibration of spectrum " + (i + 1)));
                    var mzs = calibratedSpectra.Last().Extract(p.mzRange);
                    p.OnWatch(new OutputHandlerEventArgs(string.Join(", ", mzs)));
                }

            }
            p.OnOutput(new OutputHandlerEventArgs());
            return calibratedSpectra;
        }

        public static List<double> CalibratePrecursorMZs(CalibrationFunction cf, SoftwareLockMassParams p)
        {
            List<double> calibratedPrecursorMZs = new List<double>();
            double precursorTime = -1;
            for (int i = 0; i < p.myMsDataFile.LastSpectrumNumber; i++)
            {
                if (p.myMsDataFile.LastSpectrumNumber < 100)
                    p.OnProgress(new ProgressHandlerEventArgs(i));
                else if (i % (p.myMsDataFile.LastSpectrumNumber / 100) == 0)
                    p.OnProgress(new ProgressHandlerEventArgs((i / (p.myMsDataFile.LastSpectrumNumber / 100))));
                double newMZ = -1;
                if (p.myMsDataFile.GetScan(i + 1).MsnOrder == 1)
                {
                    precursorTime = p.myMsDataFile.GetScan(i + 1).RetentionTime;
                }
                else
                {
                    newMZ = p.myMsDataFile.GetScan(i + 1).SelectedIonMonoisotopicMZ - cf.Predict(new DataPoint(p.myMsDataFile.GetScan(i + 1).SelectedIonMonoisotopicMZ, precursorTime));
                }
                calibratedPrecursorMZs.Add(newMZ);
            }
            p.OnOutput(new OutputHandlerEventArgs());
            return calibratedPrecursorMZs;
        }

    }
}
