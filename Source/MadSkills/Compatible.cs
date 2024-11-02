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
        public static Dictionary<int, float> powerCache = new Dictionary<int, float>();

        static Compatible()
        {
            Harmony harmony = new Harmony("cedaro.MadSkillsPlus");
            Execute(harmony);
        }

        // VFE compatible
        public static bool VEF = ModLister.HasActiveModWithName("Vanilla Expanded Framework");

        // VSE compatible
        public static bool VSE = ModLister.HasActiveModWithName("Vanilla Skills Expanded");

        // VFEE compatible
        public static bool VFEE = ModLister.HasActiveModWithName("Vanilla Factions Expanded - Empire");

        // Memory Implants compatible
        public static HediffDef MA = DefDatabase<HediffDef>.GetNamedSilentFail("MemoryAssistant");

        public static void Execute(Harmony harm)
        {
            if (VSE)
            {
                harm.Patch(
                    AccessTools.Method("VSE.Passions.PassionPatches:AddForgetRateInfo"),
                    prefix: new HarmonyMethod(typeof(Compatible), "VSE_AddForgetRateInfo_Prefix")
                );
                AccessTools.Field("VSE.ModCompat:MadSkills").SetValue(null, true);
                AccessTools.Field("VSE.ModCompat:saturatedXPMultiplier").SetValue(null, (Func<float>)AccessTools.PropertyGetter(typeof(ModSettings), "SaturatedXPMultiplier").CreateDelegate(typeof(Func<float>)));
                harm.CreateReversePatcher(
                    AccessTools.Method("VSE.Passions.PassionManager:ForgetRateFactor"),
                    new HarmonyMethod(typeof(Compatible), nameof(Compatible.VSE_ForgetRateFactor))
                ).Patch();
            }
            if (VEF)
            {
                harm.CreateReversePatcher(
                    AccessTools.Method("VanillaGenesExpanded.VanillaGenesExpanded_SkillRecord_Interval_Transpiler_Patch:GetMultiplier"),
                    new HarmonyMethod(typeof(Compatible), nameof(Compatible.VEF_GetMultiplier))
                ).Patch();
            }
            if (VFEE)
            {
                harm.CreateReversePatcher(
                    AccessTools.Method("VFEEmpire.HarmonyPatches.Patch_HonorsMisc:Prefix_Interval"),
                    new HarmonyMethod(typeof(Compatible), nameof(Compatible.VFEE_HonorCheck))
                ).Patch();
            }
        }

        public static float VSE_ForgetRateFactor(SkillRecord sk)
        {
            throw new NotImplementedException();
        }

        public static float VEF_GetMultiplier(Pawn p)
        {
            // if false, then skip interval
            throw new NotImplementedException();
        }

        public static bool VFEE_HonorCheck(SkillRecord sk)
        {
            throw new NotImplementedException();
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

            if (sk.GetLevel() > 20 && ModSettings.overlimitXPMultiplier != 1f)
            {
                var level = sk.GetLevel();
                if (powerCache.ContainsKey(level))
                {
                    factor *= powerCache[level];
                }
                else
                {
                    var pow = Mathf.Pow(ModSettings.overlimitXPMultiplier, level - 20);
                    powerCache[level] = pow;
                    factor *= pow;
                }
            }

            if (VSE)
            {
                factor *= VSE_ForgetRateFactor(sk);
            }

            if (VEF)
            {
                factor *= VEF_GetMultiplier(sk.Pawn);
            }

            if (VFEE && !VFEE_HonorCheck(sk))
            {
                factor *= 0f;
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

            var totalLoss = ExtraFactor(sk);
            builder.AppendLineTagged(("VSE.ForgetSpeed".Translate() + ": ").AsTipTitle() + totalLoss.ToStringPercent("F0"));

            builder.AppendLine("  - " + "StatsReport_BaseValue".Translate() + ": " + 1f.ToStringPercent("F0"));

            var passionLoss = VSE_ForgetRateFactor(sk);
            builder.AppendLine("  - " + sk.passion.GetLabel() + ": x" + passionLoss.ToStringPercent("F0"));

            if (sk.ExperiencedLevel() > 0 && ModSettings.ExperienceMultiplier != 1f)
            {
                float factor = Mathf.Pow(ModSettings.ExperienceMultiplier, sk.ExperiencedLevel());
                builder.AppendLine("  - " + "Experience".Translate() + ": x" + factor.ToStringPercent("F0"));
            }

            if (!ModSettings.greatMemoryAltered && sk.Pawn.story.traits.HasTrait(TraitDefOf.GreatMemory))
            {
                builder.AppendLine("  - " + TraitDefOf.GreatMemory.degreeDatas[0].LabelCap + ": x50%");
            }

            if (VEF && VEF_GetMultiplier(sk.Pawn) != 1f)
            {
                builder.AppendLine("  - " + "Gene".Translate() + ": x" + VEF_GetMultiplier(sk.Pawn).ToStringPercent("F0"));
            }

            if (VFEE && !VFEE_HonorCheck(sk))
            {
                builder.AppendLine("  - " + "VFEE.Honors".Translate() + ": x0%");
            }

            if (MA != null && sk.Pawn.health.hediffSet.HasHediff(MA))
            {
                builder.AppendLine("  - " + MA.LabelCap + ": x20%");
            }

            return false;
        }
    }
}
