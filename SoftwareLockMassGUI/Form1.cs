using Chemistry;
using IO.MzML;
using IO.Thermo;
using MassSpectrometry;
using Proteomics;
using SoftwareLockMass;
using Spectra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoftwareLockMassGUI
{
    public partial class Form1 : Form
    {
        public static string unimodLocation = @"unimod_tables.xml";
        public static string psimodLocation = @"PSI-MOD.obo.xml";
        public static string elementsLocation = @"elements.dat";
        public static string uniprotLocation = @"ptmlist.txt";

        public static List<AnEntry> myListOfEntries;

        private BindingList<AnEntry> binding1;

        UsefulProteomicsDatabases.unimod unimodDeserialized;
        UsefulProteomicsDatabases.obo psimodDeserialized;

        private Dictionary<int, ChemicalFormulaModification> uniprotDeseralized;

        public Form1()
        {
            InitializeComponent();

            UsefulProteomicsDatabases.Loaders.unimodLocation = unimodLocation;
            UsefulProteomicsDatabases.Loaders.psimodLocation = psimodLocation;
            UsefulProteomicsDatabases.Loaders.elementLocation = elementsLocation;
            UsefulProteomicsDatabases.Loaders.uniprotLocation = uniprotLocation;

            UsefulProteomicsDatabases.Loaders.LoadElements();
            unimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadUnimod();
            psimodDeserialized = UsefulProteomicsDatabases.Loaders.LoadPsiMod();
            uniprotDeseralized = UsefulProteomicsDatabases.Loaders.LoadUniprot();

            myListOfEntries = new List<AnEntry>();
            //myListOfEntries.Add(new AnEntry("some raw file", "some mzid file"));
            //myListOfEntries.Add(new AnEntry("some mzml file", "corresponding mzid file"));

            binding1 = new BindingList<AnEntry>(myListOfEntries); // <-- BindingList

            dataGridView1.DataSource = binding1;

            // THIS IS JUST FOR DEBUGGING   
            //origDataFile = @"E:\Stefan\data\jurkat\120426_Jurkat_highLC_Frac1.raw";
            //mzidFile = @"E:\Stefan\data\4FileExperiments\4FileExperiment10ppmForCalibration\120426_Jurkat_highLC_Frac1.mzid";

            //SoftwareLockMassRunner.p = new SoftwareLockMassParams(origDataFile, mzidFile);
            //SoftwareLockMassRunner.p.outputHandler += P_outputHandler;
            //SoftwareLockMassRunner.p.progressHandler += P_progressHandler;
            //SoftwareLockMassRunner.p.watchHandler += P_watchHandler;

            //SoftwareLockMassRunner.p.MS1spectraToWatch.Add(11187);
            //SoftwareLockMassRunner.p.MS2spectraToWatch.Add(11188);
            //SoftwareLockMassRunner.p.mzRange = new Range<double>(1113.4,1114.5);

            //SoftwareLockMassRunner.p.MS1spectraToWatch.Add(11289);
            //SoftwareLockMassRunner.p.MS2spectraToWatch.Add(11290);
            //SoftwareLockMassRunner.p.mzRange = new Range<double>(1163, 1167);

            //SoftwareLockMassRunner.p.MS1spectraToWatch.Add(5893);
            //SoftwareLockMassRunner.p.MS2spectraToWatch.Add(5894);
            //SoftwareLockMassRunner.p.mzRange = new Range<double>(948,952);

            //Thread thread = new Thread(new ThreadStart(SoftwareLockMassRunner.Run));
            //thread.IsBackground = true;
            //thread.Start();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Mass Spec Files(*.raw;*.mzML;*.mzid)|*.raw;*.mzML;*.mzid|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                addFilePaths(openFileDialog1.FileNames);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Parallel.ForEach(myListOfEntries, (anEntry) =>
             {
                 IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile;
                 if (Path.GetExtension(anEntry.spectraFile).Equals(".mzML"))
                 {
                     myMsDataFile = new Mzml(anEntry.spectraFile);
                 }
                 else
                 {
                     myMsDataFile = new ThermoRawFile(anEntry.spectraFile);
                 }
                 var a = new SoftwareLockMassParams(myMsDataFile);
                 a.outputHandler += P_outputHandler;
                 a.progressHandler += P_progressHandler;
                 a.watchHandler += P_watchHandler;
                 a.postProcessing = MzmlOutput;
                 a.getFormulaFromDictionary = getFormulaFromDictionary;
                 a.identifications = new MzidIdentifications(anEntry.mzidFile);

                 var t = new Thread(() => RealStart(a));
                 t.IsBackground = true;
                 t.Start();
             });
        }

        private static void RealStart(SoftwareLockMassParams a)
        {
            SoftwareLockMassRunner.Run(a);
        }


        private static int GetLastNumberFromString(string s)
        {
            return Convert.ToInt32(Regex.Match(s, @"\d+$").Value);
        }

        public string getFormulaFromDictionary(string dictionary, string acession)
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

        public void MzmlOutput(SoftwareLockMassParams p, List<IMzSpectrum<MzPeak>> calibratedSpectra, List<double> calibratedPrecursorMZs)
        {
            p.OnOutput(new OutputHandlerEventArgs("Creating _indexedmzMLConnection, and putting data in it"));
            MzmlMethods.CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(p.myMsDataFile, calibratedSpectra, calibratedPrecursorMZs, Path.Combine(Path.GetDirectoryName(p.myMsDataFile.FilePath), Path.GetFileNameWithoutExtension(p.myMsDataFile.FilePath) + "-Calibrated.mzML"));
        }

        private void P_watchHandler(object sender, OutputHandlerEventArgs e)
        {
            if (textBox2.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(P_watchHandler);
                Invoke(d, new object[] { sender, e });
            }
            else
            {
                textBox2.AppendText(e.output + "\n");
            }
        }

        private void P_progressHandler(object sender, ProgressHandlerEventArgs e)
        {
            if (progressBar1.InvokeRequired)
            {
                SetProgressCallback d = new SetProgressCallback(P_progressHandler);
                Invoke(d, new object[] { sender, e });
            }
            else
            {
                progressBar1.Value = Math.Min(e.progress, 100);
            }
        }

        private void P_outputHandler(object sender, OutputHandlerEventArgs e)
        {
            if (textBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(P_outputHandler);
                Invoke(d, new object[] { sender, e });
            }
            else
            {
                textBox1.AppendText(e.output + "\n");
            }
        }

        delegate void SetTextCallback(object sender, OutputHandlerEventArgs e);
        delegate void SetProgressCallback(object sender, ProgressHandlerEventArgs e);

        private void addFilePaths(string[] filepaths)
        {
            foreach (string filepath in filepaths)
            {
                ////Console.WriteLine(filepath);
                var theExtension = Path.GetExtension(filepath);
                var pathNoExtension = Path.GetFileNameWithoutExtension(filepath);
                var foundOne = false;
                foreach (AnEntry a in myListOfEntries)
                {
                    if (theExtension.Equals(".raw") || theExtension.Equals(".mzml"))
                    {
                        if (a.mzidFile != null && Path.GetFileNameWithoutExtension(a.mzidFile).Equals(pathNoExtension))
                        {
                            a.spectraFile = filepath;
                            foundOne = true;
                            dataGridView1.Refresh();
                            dataGridView1.Update();
                            break;
                        }
                    }
                    if (theExtension.Equals(".mzid"))
                    {
                        ////Console.WriteLine(Path.GetFileNameWithoutExtension(a.spectraFile));
                        ////Console.WriteLine(pathNoExtension);
                        if (a.spectraFile != null && Path.GetFileNameWithoutExtension(a.spectraFile).Equals(pathNoExtension))
                        {
                            a.mzidFile = filepath;
                            foundOne = true;
                            dataGridView1.Refresh();
                            dataGridView1.Update();
                            break;
                        }
                    }
                }
                if (!foundOne)
                {
                    ////Console.WriteLine("Adding " + filepath);
                    ////Console.WriteLine("extension " + theExtension);
                    if (theExtension.Equals(".raw") || theExtension.Equals(".mzml"))
                    {
                        ////Console.WriteLine("raw or mzml ");
                        binding1.Add(new AnEntry(filepath, null));
                    }
                    if (theExtension.Equals(".mzid"))
                    {
                        ////Console.WriteLine("mzid ");
                        binding1.Add(new AnEntry(null, filepath));
                    }
                }
            }
        }
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] filepaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            addFilePaths(filepaths);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            binding1.Clear();
        }
    }


    public class AnEntry
    {
        public AnEntry(string spectraFile, string mzidFile)
        {
            this.spectraFile = spectraFile;
            this.mzidFile = mzidFile;
        }

        [DisplayName("Spectra File")]
        public string spectraFile { get; set; }
        [DisplayName("Mzid File")]
        public string mzidFile { get; set; }
    }
}
