﻿// Copyright 2012, 2013, 2014 Derek J. Bailey
// 
// This file (ModificationCollection.cs) is part of CSMSL.
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

using Chemistry;
using Proetomics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Proteomics
{
    public class ModificationCollection : ICollection<IHasMass>, IHasMass, IEquatable<ModificationCollection>
    {
        private readonly List<IHasMass> _modifications;

        public double MonoisotopicMass { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IHasMass mod in _modifications)
            {
                sb.Append(mod);
                sb.Append(" | ");
            }
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 3, 3);
            }
            return sb.ToString();
        }

        public ModificationCollection(params IHasMass[] mods)
        {
            _modifications = mods.ToList();
            MonoisotopicMass = _modifications.Sum(m => m.MonoisotopicMass);
        }

        public void Add(IHasMass item)
        {
            _modifications.Add(item);
            MonoisotopicMass += item.MonoisotopicMass;
        }

        public void Clear()
        {
            _modifications.Clear();
            MonoisotopicMass = 0;
        }

        public bool Contains(IHasMass item)
        {
            return _modifications.Contains(item);
        }

        public void CopyTo(IHasMass[] array, int arrayIndex)
        {
            _modifications.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _modifications.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IHasMass item)
        {
            if (!_modifications.Remove(item))
                return false;
            MonoisotopicMass -= item.MonoisotopicMass;
            return true;
        }

        public IEnumerator<IHasMass> GetEnumerator()
        {
            return _modifications.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _modifications.GetEnumerator();
        }

        public override int GetHashCode()
        {
            int hCode = _modifications.GetHashCode();

            return Count + hCode;
        }

        public override bool Equals(object obj)
        {
            ModificationCollection col = obj as ModificationCollection;
            return col != null && Equals(col);
        }

        public bool Equals(ModificationCollection other)
        {
            return Count == other.Count && _modifications.ScrambledEquals(other._modifications);
        }
    }
}