using System;
using MassSpectrometry;
using System.Xml.Serialization;
using System.IO;
using System.Linq;

namespace SoftwareLockMassIO
{
    public class MzidIdentifications : Identifications
    {
        private string mzidFile;

        private mzIdentML.MzIdentMLType dd;
        public MzidIdentifications(string mzidFile)
        {
            this.mzidFile = mzidFile;
            XmlSerializer _indexedSerializer = new XmlSerializer(typeof(mzIdentML.MzIdentMLType));
            Stream stream = new FileStream(mzidFile, FileMode.Open);
            // Read the XML file into the variable
            dd = _indexedSerializer.Deserialize(stream) as mzIdentML.MzIdentMLType;
        }

        public double calculatedMassToCharge(int matchIndex)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].calculatedMassToCharge;
        }

        public int chargeState(int matchIndex)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].chargeState;

        }

        public double experimentalMassToCharge(int matchIndex)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].SpectrumIdentificationItem[0].experimentalMassToCharge;
        }

        public int getNumBelow(double thresholdPassParameter)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult.Select(b => Convert.ToDouble(b.SpectrumIdentificationItem[0].cvParam[0].value)).Where(b => b > thresholdPassParameter).Count();
        }

        public bool isDecoy(int matchIndex)
        {
            return dd.SequenceCollection.PeptideEvidence[matchIndex].isDecoy;
        }

        public string modificationAcession(int matchIndex, int i)
        {
            return dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].accession;
        }

        public string modificationDictionary(int matchIndex, int i)
        {
            return dd.SequenceCollection.Peptide[matchIndex].Modification[i].cvParam[0].cvRef;
        }

        public int modificationLocation(int matchIndex, int i)
        {
            return dd.SequenceCollection.Peptide[matchIndex].Modification[i].location;
        }

        public int NumModifications(int matchIndex)
        {
            if (dd.SequenceCollection.Peptide[matchIndex].Modification == null)
                return 0;
            return dd.SequenceCollection.Peptide[matchIndex].Modification.Length;
        }

        public string PeptideSequence(int matchIndex)
        {
            return dd.SequenceCollection.Peptide[matchIndex].PeptideSequence;
        }

        public string spectrumID(int matchIndex)
        {
            return dd.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[matchIndex].spectrumID;
        }
    }
}