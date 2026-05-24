using UnityEngine;

namespace ClassBrawl.Feature
{
    public static class MatchFormulas
    {
        public static int CalculateWinsNeeded(int matchFormat)
            => Mathf.CeilToInt(matchFormat / 2f);

        public static int CalculateMaxRounds(int winsNeeded)
            => winsNeeded * 2 - 1;

        public static bool IsMatchOver(int[] scores, int winsNeeded)
            => scores[0] >= winsNeeded || scores[1] >= winsNeeded;

        public static bool IsDraw(int[] scores, int winsNeeded)
            => scores[0] >= winsNeeded && scores[1] >= winsNeeded;

        public static int? GetWinner(int[] scores, int winsNeeded)
        {
            if (!IsMatchOver(scores, winsNeeded)) return null;
            if (IsDraw(scores, winsNeeded)) return null;
            return scores[0] >= winsNeeded ? 0 : 1;
        }

        public static int ClampMatchFormat(int format)
        {
            if (format <= 0) return 1;
            if (format % 2 == 0) return format + 1;
            return Mathf.Min(format, 5);
        }
    }
}
