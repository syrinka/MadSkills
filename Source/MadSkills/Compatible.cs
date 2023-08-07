using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace RTMadSkills
{
    [StaticConstructorOnStartup]
    public static class Compatible
    {
        static Compatible()
        {
            Harmony harmony = new Harmony("io.github.ratysz.madskills");
            Execute(harmony);
        }

        // VSE compatible
        public static bool VSE = ModLister.HasActiveModWithName("Vanilla Skills Expanded");
        public static MethodInfo ForgetRateFactor = AccessTools.Method("VSE.Passions.PassionManager:ForgetRateFactor");
        public static MethodInfo GetForgetRateFactor = AccessTools.Method("VSE.Passions.PassionPatches:GetForgetRateFactor");

        // Memory Implants compatible
        public static HediffDef MA = DefDatabase<HediffDef>.GetNamedSilentFail("MemoryAssistant");

        public static void Execute(Harmony harm)
        {
            harm.Patch(
                AccessTools.Method("VSE.Passions.PassionPatches:AddForgetRateInfo"),
                prefix: new HarmonyMethod(typeof(Compatible), "VSE_AddForgetRateInfo_Prefix"));
            harm.Patch(
                ForgetRateFactor,
                postfix: new HarmonyMethod(typeof(Compatible), "VSE_ForgetRateFactor_Postfix"));
        }

        public static float ExtraFactor(SkillRecord sk)
        {
            float factor = 1f;
            if (!ModSettings.greatMemoryAltered && sk.Pawn.story.traits.HasTrait(TraitDefOf.GreatMemory))
            {
                factor *= 0.5f;
            }
            if (sk.ExperiencedLevel() > 0)
            {
                factor *= Mathf.Pow(ModSettings.ExperienceMultiplier, sk.ExperiencedLevel());
            }
            if (MA != null && sk.Pawn.health.hediffSet.HasHediff(MA))
            {
                factor *= 0.2f;
            }
            return factor;
        }

        public static bool VSE_AddForgetRateInfo_Prefix(SkillRecord sk, StringBuilder builder)
        {
            builder.AppendLine();

            var loss1 = (float)ForgetRateFactor.Invoke(null, new object[] { sk });
            builder.AppendLineTagged(("VSE.ForgetSpeed".Translate() + ": ").AsTipTitle() + loss1.ToStringPercent());

            builder.AppendLine("  - " + "StatsReport_BaseValue".Translate() + ": " + 1f.ToStringPercent());

            var loss2 = (float)GetForgetRateFactor.Invoke(null, new object[] { sk.passion });
            builder.AppendLine("  - " + sk.passion.GetLabel() + ": x" + loss2.ToStringPercent("F0"));


            if (sk.ExperiencedLevel() > 0)
            {
                float factor = Mathf.Pow(ModSettings.ExperienceMultiplier, sk.ExperiencedLevel());
                builder.AppendLine("  - " + "Experience".Translate() + ": x" + factor.ToStringPercent("F0"));
            }

            if (!ModSettings.greatMemoryAltered && sk.Pawn.story.traits.HasTrait(TraitDefOf.GreatMemory))
            {
                builder.AppendLine("  - " + TraitDefOf.GreatMemory.degreeDatas[0].LabelCap + ": x50%");
            }
            if (MA != null && sk.Pawn.health.hediffSet.HasHediff(MA))
            {
                builder.AppendLine("  - " + MA.LabelCap + ": x20%");
            }

            return false;
        }

        public static void VSE_ForgetRateFactor_Postfix(SkillRecord skillRecord, ref float __result)
        {
            __result *= ExtraFactor(skillRecord);
        }
    }
}
