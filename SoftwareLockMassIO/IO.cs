using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using Proteomics;
using SoftwareLockMass;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SoftwareLockMassIO
{
    public static class IO
    {
        public static UsefulProteomicsDatabases.unimod unimodDeserialized;
        public static UsefulProteomicsDatabases.obo psimodDeserialized;
        public static Dictionary<int, ChemicalFormulaModification> uniprotDeseralized;

        public static string unimodLocation = @"unimod_tables.xml";
        public static string psimodLocation = @"PSI-MOD.obo.xml";
        public static string elementsLocation = @"elements.dat";
        public static string uniprotLocation = @"ptmlist.txt";

        private static int GetLastNumberFromString(string s)
        {
            return Convert.ToInt32(Regex.Match(s, @"\d+$").Value);
        }

        public static SoftwareLockMassParams GetReady(string origDataFile, EventHandler<OutputHandlerEventArgs> p_outputHandler, EventHandler<ProgressHandlerEventArgs> p_progressHandler, string mzidFile)
        {
            IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile;
            if (Path.GetExtension(origDataFile).Equals(".mzML"))
                myMsDataFile = new Mzml(origDataFile);
            else
                myMsDataFile = new ThermoRawFile(origDataFile);
            var a = new SoftwareLockMassParams(myMsDataFile);
            a.outputHandler += p_outputHandler;
            a.progressHandler += p_progressHandler;
            a.postProcessing = MzmlOutput;
            a.getFormulaFromDictionary = getFormulaFromDictionary;
            a.identifications = new MzidIdentifications(mzidFile);
            return a;
        }

        public static void Load()
        {
            UsefulProteomicsDatabases.Loaders.unimodLocation = unimodLocation;
            UsefulProteomicsDatabases.Loaders.psimodLocation = psimodLocation;
            UsefulProteomicsDatabases.Loaders.elementLocation = elementsLocation;
            UsefulProteomicsDatabases.Loaders.uniprotLocation = uniprotLocation;

            UsefulProteomicsDatabases.Loaders.LoadElements();
            unimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadUnimod();
            psimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadPsiMod();
            uniprotDeseralized = UsefulProteomicsDatabases.Loaders.LoadUniprot();
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
                    throw new Exception("Error in reading psi-mod file, acession mismatch!");
                else
                {
                    foreach (var a in ksadklfj.xref_analog)
                    {
                        if (a.dbname == "DiffFormula")
                        {
                            return a.name;
                        }
                    }
                    Console.WriteLine("Formula from uniprot: " + uniprotDeseralized[GetLastNumberFromString(psimodAcession)].thisChemicalFormula.Formula);
                    return uniprotDeseralized[GetLastNumberFromString(psimodAcession)].thisChemicalFormula.Formula;

                    //throw new Exception("Error in reading psi-mod file, could not find formula!");
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
