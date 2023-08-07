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

        public static int LastLearntTick(this SkillRecord sk)
        {
            if (LastLearntTickRecord.TryGetValue(sk, out int tick))
            {
                return tick;
            }
            return 0;
        }
    }

    [HarmonyPatch(typeof(SkillRecord))]
    [HarmonyPatch("Learn")]
    internal static class Patch_SkillRecordLearn
    {
        private static void Postfix(SkillRecord __instance, float xp, bool direct)
        {
            if (xp > 0f && !direct)
            {
                RetentionUtility.LastLearntTickRecord[__instance] = Find.TickManager.TicksGame;
            }
        }
    }

    [HarmonyPatch(typeof(SkillRecord))]
    [HarmonyPatch("ExposeData")]
    internal static class Patch_GameExposeData
    {
        private static void Postfix(SkillRecord __instance)
        {
            int tick = __instance.LastLearntTick();
            Scribe_Values.Look(ref tick, "LastLearntTick");
            RetentionUtility.LastLearntTickRecord[__instance] = tick;
        }
    }
}