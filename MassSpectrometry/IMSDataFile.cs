// Copyright 2012, 2013, 2014 Derek J. Bailey
// 
// This file (IMsDataFile.cs) is part of CSMSL.
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

using System;
using System.Collections.Generic;
using Spectra;
using MassSpectrometry.Enums;

namespace MassSpectrometry
{
    public interface IMsDataFile : IEnumerable<IMsDataScan>, IDisposable, IEquatable<IMsDataFile>
    {
        void Open();
        string Name { get; }
        bool IsOpen { get; }
        int FirstSpectrumNumber { get; }
        int LastSpectrumNumber { get; }
        int GetMsnOrder(int spectrumNumber);
        double GetInjectionTime(int spectrumNumber);
        double GetPrecursorMonoisotopicMz(int spectrumNumber);
        double GetRetentionTime(int spectrumNumber);
        DissociationType GetDissociationType(int spectrumNumber, int msnOrder = 2);
        Polarity GetPolarity(int spectrumNumber);
        ISpectrum GetSpectrum(int spectrumNumber);
        IMsDataScan this[int spectrumNumber] { get; }
    }

    public interface IMsDataFile<out TSpectrum> : IMsDataFile, IEnumerable<IMsDataScan<TSpectrum>>
        where TSpectrum : ISpectrum
    {
        new TSpectrum GetSpectrum(int spectrumNumber);
        new IMsDataScan<TSpectrum> this[int spectrumNumber] { get; }
    }
}