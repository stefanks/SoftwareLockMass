// Copyright 2012, 2013, 2014 Derek J. Bailey
// Modified work Copyright 2016 Stefan Solntsev
//
// This file (MsDataScan.cs) is part of CSMSL.
//
// CSMSL is free software: you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// CSMSL is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public
// License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with CSMSL. If not, see <http://www.gnu.org/licenses/>.

using MassSpectrometry.Enums;
using Spectra;
using System;

namespace MassSpectrometry
{
    public class MsDataScan<TSpectrum> : IMsDataScan<TSpectrum>, IEquatable<MsDataScan<TSpectrum>> 
        where TSpectrum : ISpectrum
    {
        public MsDataFile<TSpectrum> ParentFile { get; private set; }

        private TSpectrum _massMzSpectrum;

        /// <summary>
        /// The mass spectrum associated with the scan
        /// </summary>
        public TSpectrum MassSpectrum
        {
            get
            {
                if (_massMzSpectrum != null || ParentFile == null) 
                    return _massMzSpectrum;

                if (ParentFile.IsOpen)
                {
                    _massMzSpectrum = ParentFile.GetSpectrum(SpectrumNumber);
                }
                return _massMzSpectrum;
            }
            internal set { _massMzSpectrum = value; }
        }

        ISpectrum IMassSpectrum.MassSpectrum
        {
            get { return MassSpectrum; }
        }

        public int SpectrumNumber { get; protected set; }

        private double _resolution = double.NaN;

        public double Resolution
        {
            get
            {
                if (!double.IsNaN(_resolution) || ParentFile == null) 
                    return _resolution;

                if (ParentFile.IsOpen)
                {
                    _resolution = ParentFile.GetResolution(SpectrumNumber);
                }
                return _resolution;
            }
            internal set { _resolution = value; }
        }

        public int MsnOrder { get; protected set; }

        private double _injectionTime = double.NaN;

        public virtual double InjectionTime
        {
            get
            {
                if (double.IsNaN(_injectionTime))
                {
                    if (ParentFile.IsOpen)
                    {
                        _injectionTime = ParentFile.GetInjectionTime(SpectrumNumber);
                    }
                }
                return _injectionTime;
            }
            internal set { _injectionTime = value; }
        }

        private double _retentionTime = double.NaN;

        public double RetentionTime
        {
            get
            {
                if (double.IsNaN(_retentionTime))
                {
                    if (ParentFile.IsOpen)
                    {
                        _retentionTime = ParentFile.GetRetentionTime(SpectrumNumber);
                    }
                }
                return _retentionTime;
            }
            internal set { _retentionTime = value; }
        }

        private Polarity _polarity = Polarity.Neutral;

        public Polarity Polarity
        {
            get
            {
                if (_polarity == Polarity.Neutral)
                {
                    if (ParentFile.IsOpen)
                    {
                        _polarity = ParentFile.GetPolarity(SpectrumNumber);
                    }
                }
                return _polarity;
            }
            internal set { _polarity = value; }
        }

        private MZAnalyzerType _mzAnalyzer = MZAnalyzerType.Unknown;

        public MZAnalyzerType MzAnalyzer
        {
            get
            {
                if (_mzAnalyzer == MZAnalyzerType.Unknown)
                {
                    if (ParentFile.IsOpen)
                    {
                        _mzAnalyzer = ParentFile.GetMzAnalyzer(SpectrumNumber);
                    }
                }
                return _mzAnalyzer;
            }
            internal set { _mzAnalyzer = value; }
        }

        private DoubleRange _mzRange;

        public DoubleRange MzRange
        {
            get
            {
                if (_mzRange == null)
                {
                    if (ParentFile.IsOpen)
                    {
                        _mzRange = ParentFile.GetMzRange(SpectrumNumber);
                    }
                }
                return _mzRange;
            }
            internal set { _mzRange = value; }
        }

        private int _parentScanNumber = -1;

        public int ParentScanNumber
        {
            get
            {
                if (_parentScanNumber < 0)
                {
                    _parentScanNumber = ParentFile.GetParentSpectrumNumber(SpectrumNumber);
                }
                return _parentScanNumber;
            }
            internal set { _parentScanNumber = value; }
        }

        private string _scanFilter;

        public string ScanFilter
        {
            get
            {
                if (_scanFilter == null)
                {
                    if (ParentFile.IsOpen)
                    {
                        _scanFilter = ParentFile.GetScanFilter(SpectrumNumber);
                    }
                }
                return _scanFilter;
            }
            internal set { _scanFilter = value; }
        }

        private bool _isCentroid;
        public bool isCentroid
        {
            get
            {
                if (ParentFile.IsOpen)
                {
                    _isCentroid = ParentFile.GetIsCentroid(SpectrumNumber);
                }
                return _isCentroid;
            }
            internal set { _isCentroid = value; }
        }


        private string _id;
        public string id
        {
            get
            {
                if (ParentFile.IsOpen)
                {
                    _id = ParentFile.GetSpectrumID(SpectrumNumber);
                }
                return _id;
            }
            internal set { _id = value; }
        }

        private string _precursorID;
        public string PrecursorID
        {
            get
            {
                if (ParentFile.IsOpen)
                {
                    _precursorID = ParentFile.GetPrecursorID(SpectrumNumber);
                }
                return _precursorID;
            }
            internal set { _precursorID = value; }
        }

        private double _selectedIonMonoisotopicMZ;
        public double SelectedIonMonoisotopicMZ
        {
            get
            {
                    if (ParentFile.IsOpen)
                    {
                    _selectedIonMonoisotopicMZ = ParentFile.GetPrecursorMonoisotopicMz(SpectrumNumber);
                    }
                    return _selectedIonMonoisotopicMZ;
                }
            internal set { _selectedIonMonoisotopicMZ = value; }
        }

        private int _selectedIonChargeState;
        public int SelectedIonChargeState
        {
            get
            {
                    if (ParentFile.IsOpen)
                    {
                    _selectedIonChargeState = ParentFile.GetPrecusorCharge(SpectrumNumber);
                    }
                    return _selectedIonChargeState;
                }
            internal set { _selectedIonChargeState = value; }
        }

        private double _selectedIonIsolationIntensity;
        public double SelectedIonIsolationIntensity
        {
            get
            {
                if (ParentFile.IsOpen)
                {
                    _selectedIonIsolationIntensity = ParentFile.GetPrecursorIsolationIntensity(SpectrumNumber);
                }
                return _selectedIonIsolationIntensity;
            }
            internal set { _selectedIonIsolationIntensity = value; }
        }

        public MsDataScan()
        {
            MsnOrder = -1;
        }

        public MsDataScan(int spectrumNumber, int msnOrder = 1, MsDataFile<TSpectrum> parentFile = null)
        {
            SpectrumNumber = spectrumNumber;
            MsnOrder = msnOrder;
            ParentFile = parentFile;
        }

        public override string ToString()
        {
            if (ParentFile == null)
            {
                return string.Format("Scan #{0}", SpectrumNumber);
            }
            return string.Format("Scan #{0} from {1}", SpectrumNumber, ParentFile);
        }

        public override int GetHashCode()
        {
            return ParentFile.GetHashCode() ^ SpectrumNumber;
        }

        public bool Equals(MsDataScan<TSpectrum> other)
        {
            if (ReferenceEquals(this, other)) return true;
            return SpectrumNumber.Equals(other.SpectrumNumber) && ParentFile.Equals(other.ParentFile);
        }
    }
}