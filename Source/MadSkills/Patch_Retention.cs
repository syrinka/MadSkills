using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;


namespace RTMadSkills
{
    internal static class Retention
    {
        public static Dictionary<string, int> tickRecord = new Dictionary<string, int>();
        public static string RetentionID(this SkillRecord rec)
        {
            return $"{rec.Pawn.ThingID}#{rec.def.defName}";
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
                string id = __instance.RetentionID();
                Retention.tickRecord[id] = Find.TickManager.TicksGame;
            }
        }
    }

    [HarmonyPatch(typeof(Game))]
    [HarmonyPatch("ExposeData")]
    internal static class Patch_GameExposeData
    {
        private static void Postfix()
        {
            Scribe_Collections.Look(ref Retention.tickRecord, "MadSkillsPlus_RetentionTickRecord", LookMode.Value, LookMode.Value);
        }
    }

    [HarmonyPatch]
    internal static class Patch_PawnDespawn
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Pawn), "Destroy");
            yield return AccessTools.Method(typeof(Pawn), "DeSpawn");
        }

        private static void Postfix(Pawn __instance)
        {
            Retention.tickRecord.RemoveAll(t => t.Key.StartsWith(__instance.ThingID));
        }
    }
}