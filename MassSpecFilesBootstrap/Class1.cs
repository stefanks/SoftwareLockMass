using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using SoftwareLockMass;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MassSpecFilesBootstrap
{
    public class Class1
    {

        static string unimodLocation = @"C:\Users\stepa\Data\Databases\Elements\unimod_tables.xml";
        static string psimodLocation = @"C:\Users\stepa\Data\Databases\PSI-MOD\PSI-MOD.obo.xml";
        static string elementsLocation = @"C:\Users\stepa\Data\Databases\Elements\elements.dat";

        static UsefulProteomicsDatabases.unimod unimodDeserialized;
        static UsefulProteomicsDatabases.obo psimodDeserialized;

        public static void init()
        {
            UsefulProteomicsDatabases.Loaders.unimodLocation = unimodLocation;
            UsefulProteomicsDatabases.Loaders.psimodLocation = psimodLocation;
            UsefulProteomicsDatabases.Loaders.elementLocation = elementsLocation;
            UsefulProteomicsDatabases.Loaders.LoadElements();
            unimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadUnimod();
            psimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadPsiMod();
        }

        public static IMsDataFile<IMzSpectrum<MzPeak>> getFile(string spectraFile)
        {
            if (Path.GetExtension(spectraFile).Equals(".mzML"))
            {
                return new Mzml(spectraFile);
            }
            else
            {
                return new ThermoRawFile(spectraFile);
            }
        }

        public static SoftwareLockMassParams GetParams(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, string mzidFile)
        {
            var a = new SoftwareLockMassParams(myMsDataFile);
            a.postProcessing = MzmlOutput;
            a.getFormulaFromDictionary = getFormulaFromDictionary;
            a.identifications = new MzidIdentifications(mzidFile);
            return a;
        }


        private static int GetLastNumberFromString(string s)
        {
            return Convert.ToInt32(Regex.Match(s, @"\d+$").Value);
        }


        public static string getFormulaFromDictionary(string dictionary, string acession)
        {
            if (dictionary == "UNIMOD")
            {
                string unimodAcession = acession;
                var indexToLookFor = GetLastNumberFromString(unimodAcession) - 1;
                while (unimodDeserialized.modifications[indexToLookFor].record_id != GetLastNumberFromString(unimodAcession))
                    indexToLookFor--;
                return unimodDeserialized.modifications[indexToLookFor].composition;
            }
            else if (dictionary == "PSI-MOD")
            {
                string psimodAcession = acession;
                UsefulProteomicsDatabases.oboTerm ksadklfj = (UsefulProteomicsDatabases.oboTerm)psimodDeserialized.Items[GetLastNumberFromString(psimodAcession) + 2];
                if (GetLastNumberFromString(psimodAcession) != GetLastNumberFromString(ksadklfj.id))
                    throw new Exception("Error in reading psi-mod file!");
                else
                {
                    foreach (var a in ksadklfj.xref_analog)
                    {
                        if (a.dbname == "DiffFormula")
                        {
                            return a.name;
                        }
                    }
                    throw new Exception("Error in reading psi-mod file!");
                }
            }
            else
                throw new Exception("Not familiar with modification dictionary " + dictionary);
        }

        public static void MzmlOutput(SoftwareLockMassParams p, List<IMzSpectrum<MzPeak>> calibratedSpectra, List<double> calibratedPrecursorMZs)
        {
            p.OnOutput(new OutputHandlerEventArgs("Creating _indexedmzMLConnection, and putting data in it"));
            MzmlMethods.CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(p.myMsDataFile, calibratedSpectra, calibratedPrecursorMZs, Path.Combine(Path.GetDirectoryName(p.myMsDataFile.FilePath), Path.GetFileNameWithoutExtension(p.myMsDataFile.FilePath) + "-Calibrated.mzML"));

        }
    }
}
