using SoftwareLockMass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoftwareLockMassGUI
{
    public partial class Form1 : Form
    {
        private static List<AnEntry> myListOfEntries = new List<AnEntry>();
        private BindingList<AnEntry> binding1 = new BindingList<AnEntry>(myListOfEntries);

        public Form1()
        {
            InitializeComponent();

            SoftwareLockMassIO.IO.Load();

            dataGridView1.DataSource = binding1;

            dataGridView1.Columns[2].Visible = false;

        }

        private void buttonAddFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Mass Spec Files(*.raw;*.mzML;*.mzid;*PSMs.tsv)|*.raw;*.mzML;*.mzid;*PSMs.tsv|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                addFilePaths(openFileDialog1.FileNames);
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            Parallel.ForEach(myListOfEntries, (anEntry) =>
             {


                 SoftwareLockMassParams a = SoftwareLockMassIO.IO.GetReady(anEntry.spectraFile, P_outputHandler, P_progressHandler, anEntry.mzidFile);

                 if (checkBox1.Checked)
                     a.tsvFile = anEntry.tsvFile;
                 if (!checkBox2.Checked)
                     a.calibrateSpectra = false;


                 var t = new Thread(() => RealStart(a));
                 t.IsBackground = true;
                 t.Start();
             });
        }

        private static void RealStart(SoftwareLockMassParams a)
        {
            SoftwareLockMassRunner.Run(a);
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
                    if (theExtension.Equals(".raw") || theExtension.Equals(".mzML"))
                    {
                        if (a.filename().Equals(pathNoExtension))
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
                        if (a.filename().Equals(pathNoExtension))
                        {
                            a.mzidFile = filepath;
                            foundOne = true;
                            dataGridView1.Refresh();
                            dataGridView1.Update();
                            break;
                        }
                    }
                    if (theExtension.Equals(".tsv"))
                    {
                        if ((a.filename() + ".PSMs").Equals(pathNoExtension))
                        {
                            a.tsvFile = filepath;
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
                    if (theExtension.Equals(".raw") || theExtension.Equals(".mzML"))
                    {
                        ////Console.WriteLine("raw or mzml ");
                        binding1.Add(new AnEntry(filepath, null, null));
                    }
                    if (theExtension.Equals(".mzid"))
                    {
                        ////Console.WriteLine("mzid ");
                        binding1.Add(new AnEntry(null, filepath, null));
                    }
                    if (theExtension.Equals(".tsv"))
                    {
                        binding1.Add(new AnEntry(null, null, filepath));
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

        private void button2_Click(object sender, EventArgs e)
        {
            binding1.Clear();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                dataGridView1.Columns[2].Visible = true;
            else
                dataGridView1.Columns[2].Visible = false;
        }
    }


    public class AnEntry
    {
        public AnEntry(string spectraFile, string mzidFile, string tsvFile)
        {
            this.spectraFile = spectraFile;
            this.mzidFile = mzidFile;
            this.tsvFile = tsvFile;
        }

        [DisplayName("Spectra File")]
        public string spectraFile { get; set; }
        [DisplayName("Mzid File")]
        public string mzidFile { get; set; }
        [DisplayName("TSV File")]
        public string tsvFile { get; set; }

        public string filename()
        {
            if (spectraFile != null)
                return Path.GetFileNameWithoutExtension(spectraFile);
            if (mzidFile != null)
                return Path.GetFileNameWithoutExtension(mzidFile);
            if (tsvFile != null)
                return Path.GetFileNameWithoutExtension(tsvFile).Remove(Path.GetFileNameWithoutExtension(tsvFile).Length - 5);
            return null;
        }
    }
}
