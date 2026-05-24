using NUnit.Framework;
using ClassBrawl.Core;

namespace ClassBrawl.Tests.Core
{
    [TestFixture]
    public class DamageFormulasTests
    {
        [Test]
        public void CalculateDamageGain_WarriorGround_Returns12()
        {
            Assert.AreEqual(12.0f, DamageFormulas.CalculateDamageGain(12.0f));
        }

        [Test]
        public void CalculateKnockbackMagnitude_WarriorAtZeroDamage_ReturnsBaseKnockback()
        {
            float result = DamageFormulas.CalculateKnockbackMagnitude(0.05f, 0f, 8.0f);
            Assert.AreEqual(8.0f, result, 0.001f);
        }

        [Test]
        public void CalculateKnockbackMagnitude_WarriorAt100Damage_ReturnsIncreased()
        {
            float result = DamageFormulas.CalculateKnockbackMagnitude(0.05f, 100f, 8.0f);
            Assert.AreEqual(8.4f, result, 0.001f);
        }

        [Test]
        public void ToDisplayPercent_42_6_Returns42()
        {
            Assert.AreEqual(42, DamageFormulas.ToDisplayPercent(42.6f));
        }

        [Test]
        public void ToDisplayPercent_99_9_Returns99()
        {
            Assert.AreEqual(99, DamageFormulas.ToDisplayPercent(99.9f));
        }
    }
}
