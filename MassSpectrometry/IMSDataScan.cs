// Copyright 2012, 2013, 2014 Derek J. Bailey
// Modified work Copyright 2016 Stefan Solntsev
// 
// This file (IMsDataScan.cs) is part of CSMSL.
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

namespace MassSpectrometry
{
    public interface IMsDataScan : IHasMassSpectrum
    {
        int SpectrumNumber { get; }
        int MsnOrder { get; }
        double RetentionTime { get; }
        Polarity Polarity { get; }
        MZAnalyzerType MzAnalyzer { get; }
        DoubleRange MzRange { get; }
        string ScanFilter { get; }
        string id { get; }
        bool isCentroid { get; }
    }

    public interface IMsDataScan<out TSpectrum> : IMsDataScan
        where TSpectrum : ISpectrum
    {
        new TSpectrum MassSpectrum { get; }
        string PrecursorID { get; }
        int SelectedIonChargeState { get; }
        double SelectedIonIsolationIntensity { get; }
        double SelectedIonMonoisotopicMZ { get; }
    }
}