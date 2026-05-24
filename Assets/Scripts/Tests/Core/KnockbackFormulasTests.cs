using NUnit.Framework;
using UnityEngine;
using ClassBrawl.Core;
using ClassBrawl.Foundation;

namespace ClassBrawl.Tests.Core
{
    [TestFixture]
    public class KnockbackFormulasTests
    {
        [Test]
        public void CalculateKnockbackVector_AttackerLeftOfTarget_PushesRight()
        {
            var attackerPos = new Vector2(-2f, 0.75f);
            var targetPos = new Vector2(2f, 0.75f);

            var result = KnockbackFormulas.CalculateKnockbackVector(
                attackerPos, targetPos, FacingDirection.Right,
                8.4f, 2.0f, 1.0f);

            Assert.Greater(result.x, 0f);
            Assert.Greater(result.y, 0f);
        }

        [Test]
        public void CalculateKnockbackVector_SymmetricPositions_CorrectMagnitude()
        {
            var attackerPos = new Vector2(-2f, 0f);
            var targetPos = new Vector2(2f, 0f);

            var result = KnockbackFormulas.CalculateKnockbackVector(
                attackerPos, targetPos, FacingDirection.Right,
                10.0f, 1.0f, 1.0f);

            Assert.AreEqual(10.0f, result.magnitude, 0.1f);
        }
    }
}
