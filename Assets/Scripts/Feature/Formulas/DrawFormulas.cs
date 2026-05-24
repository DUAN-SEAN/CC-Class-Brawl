using System.Collections.Generic;
using UnityEngine;
using ClassBrawl.Core;

namespace ClassBrawl.Feature
{
    public static class DrawFormulas
    {
        public static void CalculateWeights(List<SkillData> pool, Dictionary<Rarity, float> rarityWeights)
        {
            // PoolWeight_i = (RarityPoolWeight_i / RarityPoolCount_inPool) * SkillRarityWeight_i
            // Then normalize so all DrawWeights sum to 1.0
        }

        public static List<SkillData> WeightedSampleWithoutReplacement(
            List<SkillData> pool, int count, System.Random rng)
        {
            var results = new List<SkillData>();
            var remaining = new List<SkillData>(pool);
            var weights = new List<float>();

            for (int i = 0; i < remaining.Count; i++)
                weights.Add(remaining[i].SkillDrawWeight);

            for (int k = 0; k < count && remaining.Count > 0; k++)
            {
                float totalWeight = 0f;
                for (int i = 0; i < weights.Count; i++)
                    totalWeight += weights[i];

                float randomValue = (float)rng.NextDouble() * totalWeight;
                float cumulative = 0f;
                int selectedIndex = remaining.Count - 1;

                for (int i = 0; i < weights.Count; i++)
                {
                    cumulative += weights[i];
                    if (randomValue <= cumulative)
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                results.Add(remaining[selectedIndex]);
                remaining.RemoveAt(selectedIndex);
                weights.RemoveAt(selectedIndex);
            }

            return results;
        }
    }
}
