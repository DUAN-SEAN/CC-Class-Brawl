using NUnit.Framework;
using ClassBrawl.Feature;

namespace ClassBrawl.Tests.Feature
{
    [TestFixture]
    public class MatchFormulasTests
    {
        [Test]
        public void CalculateWinsNeeded_Bo3_Returns2()
        {
            Assert.AreEqual(2, MatchFormulas.CalculateWinsNeeded(3));
        }

        [Test]
        public void CalculateWinsNeeded_Bo1_Returns1()
        {
            Assert.AreEqual(1, MatchFormulas.CalculateWinsNeeded(1));
        }

        [Test]
        public void CalculateMaxRounds_Bo3_Returns3()
        {
            Assert.AreEqual(3, MatchFormulas.CalculateMaxRounds(2));
        }

        [Test]
        public void IsMatchOver_Scores2_1_WinsNeeded2_ReturnsTrue()
        {
            Assert.IsTrue(MatchFormulas.IsMatchOver(new int[] { 2, 1 }, 2));
        }

        [Test]
        public void IsMatchOver_Scores1_1_WinsNeeded2_ReturnsFalse()
        {
            Assert.IsFalse(MatchFormulas.IsMatchOver(new int[] { 1, 1 }, 2));
        }

        [Test]
        public void IsDraw_Scores2_2_WinsNeeded2_ReturnsTrue()
        {
            Assert.IsTrue(MatchFormulas.IsDraw(new int[] { 2, 2 }, 2));
        }

        [Test]
        public void GetWinner_Scores2_1_WinsNeeded2_Returns0()
        {
            Assert.AreEqual(0, MatchFormulas.GetWinner(new int[] { 2, 1 }, 2));
        }

        [Test]
        public void GetWinner_Scores1_2_WinsNeeded2_Returns1()
        {
            Assert.AreEqual(1, MatchFormulas.GetWinner(new int[] { 1, 2 }, 2));
        }

        [Test]
        public void GetWinner_Draw_ReturnsNull()
        {
            Assert.IsNull(MatchFormulas.GetWinner(new int[] { 2, 2 }, 2));
        }

        [Test]
        public void ClampMatchFormat_Zero_Returns1()
        {
            Assert.AreEqual(1, MatchFormulas.ClampMatchFormat(0));
        }

        [Test]
        public void ClampMatchFormat_Even_ReturnsNextOdd()
        {
            Assert.AreEqual(3, MatchFormulas.ClampMatchFormat(2));
        }

        [Test]
        public void ClampMatchFormat_Seven_Returns5()
        {
            Assert.AreEqual(5, MatchFormulas.ClampMatchFormat(7));
        }
    }
}
