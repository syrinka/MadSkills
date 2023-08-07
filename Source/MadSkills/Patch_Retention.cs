using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;


namespace RTMadSkills
{
    internal static class RetentionUtility
    {
        public static Dictionary<SkillRecord, int> LastLearntTickRecord = new Dictionary<SkillRecord, int>();
        public static Dictionary<SkillRecord, int> MaxLevelReachedRecord = new Dictionary<SkillRecord, int>();
        public static int LastLearntTick(this SkillRecord sk)
        {
            if (LastLearntTickRecord.TryGetValue(sk, out int tick))
            {
                return tick;
            }
            return 0;
        }
        public static int MaxLevelReached(this SkillRecord sk)
        {
            if (MaxLevelReachedRecord.TryGetValue(sk, out int level))
            {
                return level;
            }
            return 0;
        }
        public static int ExperiencedLevel(this SkillRecord sk)
        {
            return sk.MaxLevelReached() - sk.Level;
        }
    }


    [HarmonyPatch(typeof(SkillRecord))]
    internal static class Patch_SkillRecord
    {
        [HarmonyPatch("Learn")]
        [HarmonyPostfix]
        private static void Learn_Postfix(SkillRecord __instance, float xp, bool direct)
        {
            if (xp > 0f && !direct)
            {
                RetentionUtility.LastLearntTickRecord[__instance] = Find.TickManager.TicksGame;
            }
            if (__instance.Level > __instance.MaxLevelReached())
            {
                RetentionUtility.MaxLevelReachedRecord[__instance] = __instance.Level;
            }
        }

        [HarmonyPatch("ExposeData")]
        [HarmonyPostfix]
        private static void ExposeData_Postfix(SkillRecord __instance)
        {
            int tick = __instance.LastLearntTick();
            Scribe_Values.Look(ref tick, "LastLearntTick");
            RetentionUtility.LastLearntTickRecord[__instance] = tick;

            int level = __instance.MaxLevelReached();
            Scribe_Values.Look(ref level, "MaxLevelReached");
            RetentionUtility.MaxLevelReachedRecord[__instance] = level;
        }
    }
}