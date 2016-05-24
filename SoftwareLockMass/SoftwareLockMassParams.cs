using Spectra;
using System;
using System.Collections.Generic;
using System.IO;

namespace SoftwareLockMass
{
    public class SoftwareLockMassParams
    {

        // Important for every setting. Realized only 0 and 0.01 give meaningful results when looking at performance
        // 0 IS BEST!!!
        public double thresholdPassParameter = 0;
        //private const double thresholdPassParameter = 0.01;

        // DO NOT GO UNDER 0.01!!!!! Maybe even increase.
        public double toleranceInMZforSearch = 0.01;
        //public double toleranceInMZforSearch = 0.032;

        // 1e5 is too sparse. 1e4 is nice, but misses one I like So using 5e3. 1e3 is nice. Try 0!
        public double intensityCutoff = 1e3;
        //public double intensityCutoff = 1e2;

        // My parameters!
        public bool MZID_MASS_DATA = false;

        #region isotopologue parameters
        // THIS PARAMETER IS FRAGILE!!!
        // TUNED TO CORRESPOND TO SPECTROMETER OUTPUT
        // BETTER SPECTROMETERS WOULD HAVE BETTER (LOWER) RESOLUIONS
        // Parameter for isotopolouge distribution searching
        public double fineResolution = 0.1;

        // Good number
        public int numIsotopologuesToConsider = 10;
        //public int numIsotopologuesToConsider = 15;

        // Higher means more discriminating at selecting training points. 
        public int numIsotopologuesNeededToBeConsideredIdentified = 3;
        //public int numIsotopologuesNeededToBeConsideredIdentified = 2;
        #endregion

        public string fileToCalibrate { get; private set; }

        public string mzidFile { get; private set; }

        private string _outputFile;
        public string outputFile
        {
            get
            {
                if (_outputFile == null)
                {
                    _outputFile = Path.Combine(Path.GetDirectoryName(fileToCalibrate), Path.GetFileNameWithoutExtension(fileToCalibrate) + "-Calibrated.mzML");
                }
                return _outputFile;
            }
            set
            {
                _outputFile = value;
            }
        }
        
        public event EventHandler<OutputHandlerEventArgs> outputHandler;
        public event EventHandler<ProgressHandlerEventArgs> progressHandler;
        public event EventHandler<OutputHandlerEventArgs> watchHandler;

        public HashSet<int> MS2spectraToWatch;
        public HashSet<int> MS1spectraToWatch;
        public IRange<double> mzRange;

        #region Methods
        public bool mzML()
        {
            if (Path.GetExtension(fileToCalibrate).Contains("raw"))
                return false;
            else
                return true;
        }
        #endregion

        #region Constructors
        public SoftwareLockMassParams(string fileToCalibrate, string mzidFile)
        {
            this.fileToCalibrate = fileToCalibrate;
            this.mzidFile = mzidFile;
            MS1spectraToWatch = new HashSet<int>();
            MS2spectraToWatch = new HashSet<int>();
        }
        #endregion
        
        public virtual void OnOutput(OutputHandlerEventArgs e)
        {
            outputHandler?.Invoke(this, e);
        }

        public virtual void OnProgress(ProgressHandlerEventArgs e)
        {
            progressHandler?.Invoke(this, e);
        }

        public virtual void OnWatch(OutputHandlerEventArgs e)
        {
            watchHandler?.Invoke(this, e);
        }

    }

    public class OutputHandlerEventArgs : EventArgs
    {
        public string output { get; private set; }
        public OutputHandlerEventArgs(string output)
        {
            this.output = output;
        }
        public OutputHandlerEventArgs()
        {
            output = "";
        }
    }

    public class ProgressHandlerEventArgs : EventArgs
    {
        public int progress { get; private set; }
        public ProgressHandlerEventArgs(int progress)
        {
            this.progress = progress;
        }
    }

}