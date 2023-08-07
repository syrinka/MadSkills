using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace RTMadSkills
{
    [HarmonyPatch(typeof(DefGenerator))]
    [HarmonyPatch("GenerateImpliedDefs_PostResolve")]
    public class ModSettingsDefJockey
    {
        private static FieldInfo greatMemoryDegreeDatasField = AccessTools.Field(typeof(TraitDef), "degreeDatas");

        private static bool valid = false;
        private static TraitDef greatMemory = null;
        private static List<TraitDegreeData> greatMemoryDegreeDatasBackup = null;
        private static List<TraitDegreeData> greatMemoryDegreeDatasNew = null;

        static void Postfix()
        {
            if (!valid)
            {
                greatMemory = DefDatabase<TraitDef>.GetNamed("GreatMemory");
                greatMemoryDegreeDatasBackup = (List<TraitDegreeData>)greatMemoryDegreeDatasField.GetValue(greatMemory);

                StatModifier modifier = new StatModifier();
                modifier.stat = DefDatabase<StatDef>.GetNamed("GlobalLearningFactor");
                modifier.value = 0.25f;
                TraitDegreeData data = new TraitDegreeData();
                data.statOffsets = new List<StatModifier>
                {
                    modifier
                };
                data.label = greatMemoryDegreeDatasBackup[0].label;
                data.description = "MadSkills_AlternativeGreatMemoryDescription".Translate();
                greatMemoryDegreeDatasNew = new List<TraitDegreeData>
                {
                    data
                };

                valid = true;
            }
            ApplyChanges(ModSettings.greatMemoryAltered);
        }

        static public void ApplyChanges(bool altered)
        {
            if (valid)
            {
                if (altered)
                {
                    greatMemory.degreeDatas = greatMemoryDegreeDatasNew;
                    Log.Message("[MadSkills]: changed behavior of Great Memory trait.");
                }
                else
                {
                    greatMemory.degreeDatas = greatMemoryDegreeDatasBackup;
                    Log.Message("[MadSkills]: restored behavior of Great Memory trait.");
                }
            }
        }
    }

    public class ModSettings : Verse.ModSettings
    {
        public static bool disableDegrade = false;
        public static bool sleepStopDecaying = false;
        public static bool greatMemoryAltered = true;
        private static int decayMultiplierPercentage = 0;
        private static int saturatedXPMultiplierPercentage = 20;
        public static float DecayMultiplier
        {
            get
            {
                return decayMultiplierPercentage / 100.0f;
            }
            set
            {
                decayMultiplierPercentage = Mathf.RoundToInt(DecayMultiplier * 100);
            }
        }
        public static float SaturatedXPMultiplier
        {
            get
            {
                return saturatedXPMultiplierPercentage / 100.0f;
            }
            set
            {
                saturatedXPMultiplierPercentage = Mathf.RoundToInt(DecayMultiplier * 100);
            }
        }
        public static float dailyXPSaturationThreshold = 4000.0f;
        public static float retentionHours = 0f;

        public override void ExposeData()
        {
            float multiplier_shadow = DecayMultiplier;
            float saturatedXPMultiplier_shadow = SaturatedXPMultiplier;
            Scribe_Values.Look(ref disableDegrade, "disableDegrade");
            Scribe_Values.Look(ref sleepStopDecaying, "sleepStopDecaying");
            Scribe_Values.Look(ref greatMemoryAltered, "greatMemoryAltered");
            Scribe_Values.Look(ref retentionHours, "retentionHours");
            Scribe_Values.Look(ref multiplier_shadow, "decayMultiplier");
            Scribe_Values.Look(ref saturatedXPMultiplier_shadow, "saturatedXPMultiplier");
            Scribe_Values.Look(ref dailyXPSaturationThreshold, "dailyXPSaturationThreshold");
            Log.Message("[MadSkills]: settings initialized, multiplier is " + multiplier_shadow
                + ", " + (disableDegrade ? "enable" : "disable") + " degradation"
                + ", daily XP threshold is " + dailyXPSaturationThreshold
                + ", saturated XP multiplier is " + SaturatedXPMultiplier
                + ", Great Memory trait is " + (greatMemoryAltered ? "" : "not ") + "altered.");
            ModSettingsDefJockey.ApplyChanges(greatMemoryAltered);
            decayMultiplierPercentage = Mathf.RoundToInt(multiplier_shadow * 100);
            saturatedXPMultiplierPercentage = Mathf.RoundToInt(saturatedXPMultiplier_shadow * 100);
            base.ExposeData();
        }

        public string SettingsCategory()
        {
            return "MadSkills_SettingsCategory".Translate();
        }

        public void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard list = new Listing_Standard(GameFont.Small);
            list.ColumnWidth = rect.width / 2;
            list.Begin(rect);
            list.Gap();
            {
                string buffer = decayMultiplierPercentage.ToString();
                Rect rectLine = list.GetRect(Text.LineHeight);
                Rect rectLeft = rectLine.LeftHalf().Rounded();
                Rect rectRight = rectLine.RightHalf().Rounded();
                Rect rectPercent = rectRight.RightPartPixels(Text.LineHeight);
                rectRight = rectRight.LeftPartPixels(rectRight.width - Text.LineHeight);
                Widgets.DrawHighlightIfMouseover(rectLine);
                TooltipHandler.TipRegion(rectLine, "MadSkills_DecayMultiplierTip".Translate());
                TextAnchor anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rectLeft, "MadSkills_DecayMultiplierLabel".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref decayMultiplierPercentage, ref buffer, 0, 10000);
                Widgets.Label(rectPercent, "%");
            }
            list.CheckboxLabeled(
                "MadSkills_DisableDegradeLabel".Translate(),
                ref disableDegrade,
                "MadSkills_DisableDegradeTip".Translate());
            list.CheckboxLabeled(
                "MadSkills_SleepStopDecayingLabel".Translate(),
                ref sleepStopDecaying,
                "MadSkills_SleepStopDecayingTip".Translate());
            list.CheckboxLabeled(
                "MadSkills_AlterGreatMemoryLabel".Translate(),
                ref greatMemoryAltered,
                "MadSkills_AlterGreatMemoryTip".Translate());
            list.Gap();
            {
                string buffer = retentionHours.ToString();
                Rect rectLine = list.GetRect(Text.LineHeight);
                Rect rectLeft = rectLine.LeftHalf().Rounded();
                Rect rectRight = rectLine.RightHalf().Rounded();
                Widgets.DrawHighlightIfMouseover(rectLine);
                TooltipHandler.TipRegion(rectLine, "MadSkills_RetentionHoursTip".Translate());
                TextAnchor anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rectLeft, "MadSkills_RetentionHoursLabel".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref retentionHours, ref buffer, 0, 100000);
            }
            {
                string buffer = dailyXPSaturationThreshold.ToString();
                Rect rectLine = list.GetRect(Text.LineHeight);
                Rect rectLeft = rectLine.LeftHalf().Rounded();
                Rect rectRight = rectLine.RightHalf().Rounded();
                Widgets.DrawHighlightIfMouseover(rectLine);
                TooltipHandler.TipRegion(rectLine, "MadSkills_DailyXPSaturationThresholdTip".Translate());
                TextAnchor anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rectLeft, "MadSkills_DailyXPSaturationThresholdLabel".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref dailyXPSaturationThreshold, ref buffer, 0, 100000);
            }
            {
                string buffer = saturatedXPMultiplierPercentage.ToString();
                Rect rectLine = list.GetRect(Text.LineHeight);
                Rect rectLeft = rectLine.LeftHalf().Rounded();
                Rect rectRight = rectLine.RightHalf().Rounded();
                Rect rectPercent = rectRight.RightPartPixels(Text.LineHeight);
                rectRight = rectRight.LeftPartPixels(rectRight.width - Text.LineHeight);
                Widgets.DrawHighlightIfMouseover(rectLine);
                TooltipHandler.TipRegion(rectLine, "MadSkills_SaturatedXPMultiplierTip".Translate());
                TextAnchor anchorBuffer = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rectLeft, "MadSkills_SaturatedXPMultiplierLabel".Translate());
                Text.Anchor = anchorBuffer;
                Widgets.TextFieldNumeric(rectRight, ref saturatedXPMultiplierPercentage, ref buffer, 0, 10000);
                Widgets.Label(rectPercent, "%");
            }
            list.End();
        }
    }
}
