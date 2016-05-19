using System.IO;

namespace SoftwareLockMass
{
    public class SoftwareLockMassParams
    {
        public string fileToCalibrate { get; private set; }

        public string mzidFile { get; private set; }

        private string _outputFile;
        public string outputFile
        {
            get
            {
                if (_outputFile == null)
                    _outputFile = Path.GetDirectoryName(fileToCalibrate) + Path.GetFileNameWithoutExtension(fileToCalibrate) + "-Calibrated.mzML";
                return _outputFile;
            }
            set
            {
                _outputFile = value;
            }
        }
        
        #region Constructors
        public SoftwareLockMassParams(string fileToCalibrate, string mzidFile)
        {
            this.fileToCalibrate = fileToCalibrate;
            this.mzidFile = mzidFile;
        } 
        #endregion

    }
}