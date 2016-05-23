// Copyright 2012, 2013, 2014 Derek J. Bailey
// 
// This file (AminoAcid.cs) is part of CSMSL.
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
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Proteomics
{
    public class AminoAcid : IAminoAcid
    {

        private static readonly Dictionary<string, AminoAcid> Residues;

        private static readonly AminoAcid[] ResiduesByLetter;

        /// <summary>
        /// Get the residue based on the residues's symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static AminoAcid GetResidue(string symbol)
        {
            return symbol.Length == 1 ? ResiduesByLetter[symbol[0]] : Residues[symbol];
        }

        /// <summary>
        /// Gets the resdiue based on the residue's one-character symbol
        /// </summary>
        /// <param name="letter"></param>
        /// <returns></returns>
        public static AminoAcid GetResidue(char letter)
        {
            return ResiduesByLetter[letter];
        }

        public static bool TryGetResidue(char letter, out AminoAcid residue)
        {
            residue = null;
            if (letter > 'z' || letter < 0)
                return false;
            residue = ResiduesByLetter[letter];
            return residue != null;
        }

        public static bool TryGetResidue(string symbol, out AminoAcid residue)
        {
            return Residues.TryGetValue(symbol, out residue);
        }

        public static void AddResidue(string name, char oneLetterAbbreviation, string threeLetterAbbreviation, string chemicalFormula, ModificationSites site)
        {
            AddResidueToDictionary(new AminoAcid(name, oneLetterAbbreviation, threeLetterAbbreviation, chemicalFormula, site));
        }

        /// <summary>
        /// Construct the actual amino acids
        /// </summary>
        static AminoAcid()
        {
            Residues = new Dictionary<string, AminoAcid>(66);
            ResiduesByLetter = new AminoAcid['z' + 1]; //Make it big enough for all the Upper and Lower characters
            AddResidue("Alanine", 'A', "Ala", "C3H5NO", ModificationSites.A);
            AddResidue("Arginine", 'R', "Arg", "C6H12N4O", ModificationSites.R);
            AddResidue("Asparagine", 'N', "Asn", "C4H6N2O2", ModificationSites.N);
            AddResidue("Aspartic Acid", 'D', "Asp", "C4H5NO3", ModificationSites.D);
            AddResidue("Cysteine", 'C', "Cys", "C3H5NOS", ModificationSites.C);
            AddResidue("Glutamic Acid", 'E', "Glu", "C5H7NO3", ModificationSites.E);
            AddResidue("Glutamine", 'Q', "Gln", "C5H8N2O2", ModificationSites.Q);
            AddResidue("Glycine", 'G', "Gly", "C2H3NO", ModificationSites.G);
            AddResidue("Histidine", 'H', "His", "C6H7N3O", ModificationSites.H);
            AddResidue("Isoleucine", 'I', "Ile", "C6H11NO", ModificationSites.I);
            AddResidue("Leucine", 'L', "Leu", "C6H11NO", ModificationSites.L);
            AddResidue("Lysine", 'K', "Lys", "C6H12N2O", ModificationSites.K);
            AddResidue("Methionine", 'M', "Met", "C5H9NOS", ModificationSites.M);
            AddResidue("Phenylalanine", 'F', "Phe", "C9H9NO", ModificationSites.F);
            AddResidue("Proline", 'P', "Pro", "C5H7NO", ModificationSites.P);
            AddResidue("Selenocysteine", 'U', "Sec", "C3H5NOSe", ModificationSites.U);
            AddResidue("Serine", 'S', "Ser", "C3H5NO2", ModificationSites.S);
            AddResidue("Threonine", 'T', "Thr", "C4H7NO2", ModificationSites.T);
            AddResidue("Tryptophan", 'W', "Trp", "C11H10N2O", ModificationSites.W);
            AddResidue("Tyrosine", 'Y', "Try", "C9H9NO2", ModificationSites.Y);
            AddResidue("Valine", 'V', "Val", "C5H9NO", ModificationSites.V);
        }

        private static void AddResidueToDictionary(AminoAcid residue)
        {
            Residues.Add(residue.Letter.ToString(CultureInfo.InvariantCulture), residue);
            Residues.Add(residue.Name, residue);
            Residues.Add(residue.Symbol, residue);
            ResiduesByLetter[residue.Letter] = residue;
            ResiduesByLetter[Char.ToLower(residue.Letter)] = residue;
        }

        internal AminoAcid(string name, char oneLetterAbbreviation, string threeLetterAbbreviation, string chemicalFormula, ModificationSites site)
            : this(name, oneLetterAbbreviation, threeLetterAbbreviation, new ChemicalFormula(chemicalFormula), site)
        {
        }

        internal AminoAcid(string name, char oneLetterAbbreviation, string threeLetterAbbreviation, ChemicalFormula chemicalFormula, ModificationSites site)
        {
            Name = name;
            Letter = oneLetterAbbreviation;
            Symbol = threeLetterAbbreviation;
            thisChemicalFormula = chemicalFormula;
            MonoisotopicMass = thisChemicalFormula.MonoisotopicMass;
            Site = site;
        }

        public ChemicalFormula thisChemicalFormula { get; private set; }

        public char Letter { get; private set; }

        public ModificationSites Site { get; private set; }

        public double MonoisotopicMass { get; private set; }

        public string Name { get; private set; }

        public string Symbol { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} {1} ({2})", Letter, Symbol, Name);
        }

        public ChemicalFormulaModification ToHeavyModification(bool c, bool n)
        {
            var formula = new ChemicalFormula();
            if (c)
            {
                Element carbon = PeriodicTable.GetElement("C");
                int carbon12 = thisChemicalFormula.Count(carbon[12]);
                formula.Add(carbon[12], -carbon12);
                formula.Add(carbon[13], carbon12);
            }

            if (n)
            {
                Element nitrogen = PeriodicTable.GetElement("N");
                int nitrogen14 = thisChemicalFormula.Count(nitrogen[14]);
                formula.Add(nitrogen[14], -nitrogen14);
                formula.Add(nitrogen[15], nitrogen14);
            }

            return new ChemicalFormulaModification(formula, "#", Site);
        }
    }
}