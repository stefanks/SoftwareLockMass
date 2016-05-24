using NUnit.Framework;
using SoftwareLockMass;

namespace Test
{
    [TestFixture]
    public sealed class TestSoftwareLockMass
    {
        [Test]
        public void DefaultOutputFileCheck()
        {
            SoftwareLockMassParams p = new SoftwareLockMassParams("a.mzML", "b.mzid");
            Assert.AreEqual("a-Calibrated.mzML", p.outputFile);
        }
    }
}