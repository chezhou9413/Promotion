using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace PromotionLib.Patches
{
    public static class customArmorDraw
    {
        [HarmonyPatch(typeof(Widgets))]
        public static class Widgets_ListSeparator_Patch
        {
            public static MethodInfo TargetMethod()
            {
                return AccessTools.Method(
                    typeof(Widgets),
                    "ListSeparator",
                    new Type[] { typeof(float).MakeByRefType(), typeof(float), typeof(string) }
                );
            }

            public static void Postfix(ref float curY, float width, string label)
            {
                if (label != "OverallArmor".Translate())
                {
                    return;
                }
                Pawn pawn = Find.Selector.SingleSelectedThing as Pawn;
                if (pawn == null)
                {
                    return;
                }
                Rect listingRect = new Rect(0f, curY, width, 80f); // 高度可以给个大概值
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(listingRect);
                listing.Label(ValueDef.Disinfection_level.label + ":" + pawn.GetStatValue(ValueDef.Disinfection_level));
                listing.Label(ValueDef.Sealing_level.label + ":" + pawn.GetStatValue(ValueDef.Sealing_level));
                listing.End();
                curY += listing.CurHeight;
            }
        }
    }
}
