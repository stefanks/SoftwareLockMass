using MassSpectrometry;
using Spectra;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SoftwareLockMass
{
    public class SoftwareLockMassParams
    {
        #region isotopologue parameters
        // THIS PARAMETER IS FRAGILE!!!
        // TUNED TO CORRESPOND TO SPECTROMETER OUTPUT
        // BETTER SPECTROMETERS WOULD HAVE BETTER (LOWER) RESOLUIONS
        // Parameter for isotopolouge distribution searching
        public double fineResolution = 0.1;

        // 10 is a good number. Lower would be faster!
        public int numIsotopologuesToConsider = 10;

        // Higher means more discriminating at selecting training points. 
        public int numIsotopologuesNeededToBeConsideredIdentified = 3;
        //public int numIsotopologuesNeededToBeConsideredIdentified = 2;
        #endregion

        public event EventHandler<OutputHandlerEventArgs> outputHandler;
        public event EventHandler<ProgressHandlerEventArgs> progressHandler;
        public event EventHandler<OutputHandlerEventArgs> watchHandler;

        public HashSet<int> MS2spectraToWatch;
        public HashSet<int> MS1spectraToWatch;
        public IRange<double> mzRange;

        public IMsDataFile<IMzSpectrum<MzPeak, MzRange>> myMsDataFile;
        public Identifications identifications;

        public delegate void PostProcessing(SoftwareLockMassParams p, List<IMzSpectrum<MzPeak, MzRange>> calibratedSpectra, List<double> calibratedPrecursorMZs);
        public PostProcessing postProcessing;

        public delegate string GetFormulaFromDictionary(string dictionary, string acession);
        public GetFormulaFromDictionary getFormulaFromDictionary;
        public string tsvFile = null;
        public bool calibrateSpectra = true;

        #region Constructors

        public SoftwareLockMassParams(IMsDataFile<IMzSpectrum<MzPeak, MzRange>> myMsDataFile)
        {
            this.myMsDataFile = myMsDataFile;
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
            Debug.Assert(progress >= 0 && progress <= 100);
            this.progress = progress;
        }
    }
}