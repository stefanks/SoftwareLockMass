using SoftwareLockMass;
using Spectra;
using System;
using System.Threading;
using System.Windows.Forms;
using UsefulProteomicsDatabases;

namespace SoftwareLockMassGUI
{
    public partial class Form1 : Form
    {
        private string origDataFile;
        private string mzidFile;

        public static string unimodLocation = @"E:\Stefan\data\Unimod\unimod_tables.xml";
        public static string psimodLocation = @"E:\Stefan\data\PSI-MOD\PSI-MOD.obo.xml";
        public static string elementsLocation = @"E:\Stefan\data\Elements\elements.dat";

        public Form1()
        {
            InitializeComponent();
            Loaders.unimodLocation = unimodLocation;
            Loaders.psimodLocation = psimodLocation;
            Loaders.elementLocation = elementsLocation;
            Loaders.LoadElements();
            
            // THIS IS JUST FOR DEBUGGING   
            origDataFile = @"E:\Stefan\data\jurkat\120426_Jurkat_highLC_Frac1.raw";
            mzidFile = @"E:\Stefan\data\4FileExperiments\4FileExperiment10ppmForCalibration\120426_Jurkat_highLC_Frac1.mzid";

            SoftwareLockMassRunner.p = new SoftwareLockMassParams(origDataFile, mzidFile);
            SoftwareLockMassRunner.p.outputHandler += P_outputHandler;
            SoftwareLockMassRunner.p.progressHandler += P_progressHandler;
            SoftwareLockMassRunner.p.watchHandler += P_watchHandler;

            //SoftwareLockMassRunner.p.MS1spectraToWatch.Add(11187);
            //SoftwareLockMassRunner.p.MS2spectraToWatch.Add(11188);
            //SoftwareLockMassRunner.p.mzRange = new Range<double>(1113.4,1114.5);

            //SoftwareLockMassRunner.p.MS1spectraToWatch.Add(11289);
            //SoftwareLockMassRunner.p.MS2spectraToWatch.Add(11290);
            //SoftwareLockMassRunner.p.mzRange = new Range<double>(1163, 1167);

            //SoftwareLockMassRunner.p.MS1spectraToWatch.Add(5893);
            //SoftwareLockMassRunner.p.MS2spectraToWatch.Add(5894);
            //SoftwareLockMassRunner.p.mzRange = new Range<double>(948,952);

            Thread thread = new Thread(new ThreadStart(SoftwareLockMassRunner.Run));
            thread.IsBackground = true;
            thread.Start();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Mass Spec Files(*.raw;*.mzML)|*.raw;*.mzML|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                origDataFile = openFileDialog1.FileName;
                label1.Text = "File to calibrate: " + origDataFile;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "mzid files(*.mzid)|*.mzid|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                mzidFile = openFileDialog1.FileName;
                label2.Text = "mzid file: " + mzidFile;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            SoftwareLockMassRunner.p = new SoftwareLockMassParams(origDataFile, mzidFile);
            SoftwareLockMassRunner.p.outputHandler += P_outputHandler;
            SoftwareLockMassRunner.p.progressHandler += P_progressHandler;
            SoftwareLockMassRunner.p.watchHandler += P_watchHandler;

            Thread thread = new Thread(new ThreadStart(SoftwareLockMassRunner.Run));
            thread.IsBackground = true;
            thread.Start();

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
                Invoke(d, new object[] { sender,  e });
            }
            else
            {
                textBox1.AppendText(e.output + "\n");
            }
        }

        delegate void SetTextCallback(object sender, OutputHandlerEventArgs e);
        delegate void SetProgressCallback(object sender, ProgressHandlerEventArgs e);
        
    }
}
