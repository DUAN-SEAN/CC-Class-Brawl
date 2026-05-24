using NUnit.Framework;
using ClassBrawl.Core;

namespace ClassBrawl.Tests.Core
{
    [TestFixture]
    public class FocusFormulasTests
    {
        [Test]
        public void CalculateFocusGain_WarriorGroundAttacker_Returns3_6()
        {
            float result = FocusFormulas.CalculateFocusGain(12.0f, 0.30f);
            Assert.AreEqual(3.6f, result, 0.001f);
        }

        [Test]
        public void CalculateFocusGain_WarriorGroundDefender_Returns1_2()
        {
            float result = FocusFormulas.CalculateFocusGain(12.0f, 0.10f);
            Assert.AreEqual(1.2f, result, 0.001f);
        }

        [Test]
        public void CalculateUnlockThreshold_FirstUnlock_Returns40()
        {
            float result = FocusFormulas.CalculateUnlockThreshold(0, 40.0f, 5.0f);
            Assert.AreEqual(40.0f, result, 0.001f);
        }

        [Test]
        public void CalculateUnlockThreshold_ThirdUnlock_Returns50()
        {
            float result = FocusFormulas.CalculateUnlockThreshold(2, 40.0f, 5.0f);
            Assert.AreEqual(50.0f, result, 0.001f);
        }

        [Test]
        public void ClampFocus_OverCap_ClampsToCap()
        {
            float result = FocusFormulas.ClampFocus(58.0f, 55.0f);
            Assert.AreEqual(55.0f, result, 0.001f);
        }
    }
}
