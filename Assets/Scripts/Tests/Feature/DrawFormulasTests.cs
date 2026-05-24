using System;
using System.Collections.Generic;
using NUnit.Framework;
using ClassBrawl.Core;
using ClassBrawl.Feature;

namespace ClassBrawl.Tests.Feature
{
    [TestFixture]
    public class DrawFormulasTests
    {
        [Test]
        public void WeightedSampleWithoutReplacement_PoolOf3_Count3_ReturnsAll3()
        {
            var pool = CreateTestPool(3);
            var rng = new System.Random(42);

            var result = DrawFormulas.WeightedSampleWithoutReplacement(pool, 3, rng);

            Assert.AreEqual(3, result.Count);
            CollectionAssert.AllItemsAreUnique(result);
        }

        [Test]
        public void WeightedSampleWithoutReplacement_Count1_Returns1()
        {
            var pool = CreateTestPool(3);
            var rng = new System.Random(42);

            var result = DrawFormulas.WeightedSampleWithoutReplacement(pool, 1, rng);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void WeightedSampleWithoutReplacement_PoolOf1_Returns1()
        {
            var pool = CreateTestPool(1);
            var rng = new System.Random(42);

            var result = DrawFormulas.WeightedSampleWithoutReplacement(pool, 3, rng);

            Assert.AreEqual(1, result.Count);
        }

        private static List<SkillData> CreateTestPool(int count)
        {
            var pool = new List<SkillData>();
            for (int i = 0; i < count; i++)
            {
                var skill = SkillData.CreateInstance<SkillData>();
                skill.SkillId = $"test_skill_{i}";
                skill.DisplayName = $"Test Skill {i}";
                skill.Rarity = Rarity.Common;
                skill.SkillDrawWeight = 1.0f;
                pool.Add(skill);
            }
            return pool;
        }
    }
}
