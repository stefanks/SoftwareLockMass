using SoftwareLockMass;
using System;
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
            SoftwareLockMassRunner.Run();

        }
    }
}
