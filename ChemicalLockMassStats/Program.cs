using CSMSL.IO;
using CSMSL.IO.Thermo;
using CSMSL.Spectral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChemicalLockMassStats
{
    class Program
    {
        //const string origDataFile = @"E:\Stefan\data\RawFilesWithLockMass\10-13-15_mid-blank_H2O.raw";
        //const string origDataFile = @"E:\Stefan\data\RawFilesWithLockMass\10-13-15_pre-blank_H2O.raw";
        //const string origDataFile = @"E:\Stefan\data\RawFilesWithLockMass\10-13-15_B_fract4_SID_no-lock.raw";
        const string origDataFile = @"E:\Stefan\data\RawFilesWithLockMass\10-15-15_B_fract5_rep1.raw";
        const double mz1 = 572.20593;
        const double mz2 = 589.23248;
        const double mzWidth = 0.01;

        static void Main(string[] args)
        {
            Console.WriteLine("Reading raw file");

            IMSDataFile<ISpectrum<IPeak>> myMSDataFile = new ThermoRawFile(origDataFile);
            myMSDataFile.Open();

            List<double> theMZs = new List<double>();
            List<double> theMZ2s = new List<double>();
            double bestCurrent;
            for (int i = 1; i <= myMSDataFile.LastSpectrumNumber; i++)
            {
                bestCurrent = 0;
                double theMZ = 0;
                for (int j = 0; j < myMSDataFile[i].MassSpectrum.Count; j++)
                {
                    if (myMSDataFile[i].MassSpectrum[j].X < mz1 - mzWidth)
                        continue;
                    if (myMSDataFile[i].MassSpectrum[j].X > mz1 + mzWidth)
                        break;
                    if (myMSDataFile[i].MassSpectrum[j].Y > bestCurrent)
                    {
                        bestCurrent = myMSDataFile[i].MassSpectrum[j].Y;
                        theMZ = myMSDataFile[i].MassSpectrum[j].X;
                    }
                }
                theMZs.Add(theMZ);
                bestCurrent = 0;
                double theMZ2 = 0;
                for (int j = 0; j < myMSDataFile[i].MassSpectrum.Count; j++)
                {
                    if (myMSDataFile[i].MassSpectrum[j].X < mz2 - mzWidth)
                        continue;
                    if (myMSDataFile[i].MassSpectrum[j].X > mz2 + mzWidth)
                        break;
                    if (myMSDataFile[i].MassSpectrum[j].Y > bestCurrent)
                    {
                        bestCurrent = myMSDataFile[i].MassSpectrum[j].Y;
                        theMZ2 = myMSDataFile[i].MassSpectrum[j].X;
                    }
                }
                theMZ2s.Add(theMZ2);
            }

            using (System.IO.StreamWriter file =
              new System.IO.StreamWriter(@"E:\Stefan\data\RawFilesWithLockMass\mzsfract5_rep1.dat"))
            {
                foreach (double d in theMZs)
                {
                    file.WriteLine(d);
                }
            }

            using (System.IO.StreamWriter file =
              new System.IO.StreamWriter(@"E:\Stefan\data\RawFilesWithLockMass\mzs2fract5_rep1.dat"))
            {
                foreach (double d in theMZ2s)
                {
                    file.WriteLine(d);
                }
            }
        }
    }
}
