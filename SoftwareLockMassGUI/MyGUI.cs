using SoftwareLockMass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoftwareLockMassGUI
{
    public partial class MyGUI : Form
    {
        private BindingList<AnEntry> myListOfEntries = new BindingList<AnEntry>(new List<AnEntry>());

        readonly double intensityCutoff = 1e3;
        readonly double toleranceInMZforSearch = 0.01;

        public MyGUI()
        {
            InitializeComponent();

            SoftwareLockMassIO.IO.Load();

            dataGridView1.DataSource = myListOfEntries;

            dataGridView1.Columns[3].Visible = false;

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
                 SoftwareLockMassParams a = SoftwareLockMassIO.IO.GetReady(anEntry.spectraFile, P_outputHandler, P_progressHandler, P_watchHandler, anEntry.mzidFile, intensityCutoff, toleranceInMZforSearch);

                 if (checkBox1.Checked)
                     a.tsvFile = anEntry.tsvFile;
                 if (!checkBox2.Checked)
                     a.calibrateSpectra = false;

                 var t = new Thread(() => SoftwareLockMassRunner.Run(a));
                 t.IsBackground = true;
                 t.Start();
             });
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
                progressBar1.Value = e.progress;
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
                ////// Console.WriteLine(filepath);
                var theExtension = Path.GetExtension(filepath);
                var pathNoExtension = Path.GetFileNameWithoutExtension(filepath);
                var foundOne = false;
                foreach (AnEntry a in myListOfEntries)
                {
                    if (theExtension.Equals(".raw") || theExtension.Equals(".mzML"))
                    {
                        if (a.Filename.Equals(pathNoExtension))
                        {
                            a.spectraFile = filepath;
                            foundOne = true;
                            break;
                        }
                    }
                    if (theExtension.Equals(".mzid"))
                    {
                        if (a.Filename.Equals(pathNoExtension))
                        {
                            a.mzidFile = filepath;
                            foundOne = true;
                            break;
                        }
                    }
                    if (theExtension.Equals(".tsv"))
                    {
                        if ((a.Filename + ".PSMs").Equals(pathNoExtension))
                        {
                            a.tsvFile = filepath;
                            foundOne = true;
                            break;
                        }
                    }
                }
                if (!foundOne)
                {
                    if (theExtension.Equals(".raw") || theExtension.Equals(".mzML"))
                    {
                        myListOfEntries.Add(new AnEntry(filepath, null, null));
                    }
                    if (theExtension.Equals(".mzid"))
                    {
                        myListOfEntries.Add(new AnEntry(null, filepath, null));
                    }
                    if (theExtension.Equals(".tsv"))
                    {
                        myListOfEntries.Add(new AnEntry(null, null, filepath));
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
            myListOfEntries.Clear();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                dataGridView1.Columns[3].Visible = true;
            else
                dataGridView1.Columns[3].Visible = false;
        }
    }


    public class AnEntry : INotifyPropertyChanged
    {
        public AnEntry(string spectraFile, string mzidFile, string tsvFile)
        {
            this.spectraFile = spectraFile;
            this.mzidFile = mzidFile;
            this.tsvFile = tsvFile;
        }


        string _spectraFile;
        [Browsable(false)]
        public string spectraFile
        {
            get { return _spectraFile; }
            set { SetField(ref _spectraFile, value); }
        }

        string _mzidFile;
        [Browsable(false)]
        public string mzidFile
        {
            get { return _mzidFile; }
            set { SetField(ref _mzidFile, value); }
        }


        string _tsvFile;
        [Browsable(false)]
        public string tsvFile
        {
            get { return _tsvFile; }
            set { SetField(ref _tsvFile, value); }
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Filename
        {
            get
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

        [DisplayName("Spectra File")]
        public bool spectraFileExists
        {
            get { return _spectraFile != null; }
        }
        [DisplayName("Mzid File")]
        public bool mzidFileExists
        {
            get { return _mzidFile != null; }
        }
        [DisplayName("TSV File")]
        public bool tsvFileExists
        {
            get { return _tsvFile != null; }
        }
    }
}
