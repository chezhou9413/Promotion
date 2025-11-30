// File: Patches.cs
using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace PromotionLib
{
    public static class RaceLSMPatche
    {
        private const string TargetDefName = "Luoshumeng"; // 确保和ThingDef.defName完全一致

        [HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
        public static class UnionCloneAssault
        {
            // 突击兵配置类实例
            private static readonly UnionCloneAssaultConfig config = new UnionCloneAssaultConfig();

            [HarmonyPostfix]
            public static void Postfix(Pawn __result)
            {
                if (__result?.def?.defName == TargetDefName)
                {
                  
                }
            }
        }

        // 疼痛恒为0：打在 HediffSet.PainTotal 的 getter 上
        [HarmonyPatch(typeof(HediffSet), nameof(HediffSet.PainTotal), MethodType.Getter)]
        public static class PainTotal_Getter_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(HediffSet __instance, ref float __result)
            {
                var pawn = TryGetPawn(__instance);
                if (pawn?.def?.defName == TargetDefName)
                    __result = 0f;
            }
        }

        // —— 注意：CurLevel 声明在 Need 上，而不是 Need_Rest / Need_Food ——

        // 写入时强制保持100%
        [HarmonyPatch(typeof(Need), nameof(Need.CurLevel), MethodType.Setter)]
        public static class Need_CurLevel_Setter_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Need __instance, ref float value)
            {
                var pawn = TryGetPawnFromNeed(__instance);
                if (pawn?.def?.defName == TargetDefName && (__instance is Need_Rest || __instance is Need_Food))
                {
                    if (value < 1f) value = 1f;
                }
            }
        }

        // 读取时永远返回100%
        [HarmonyPatch(typeof(Need), nameof(Need.CurLevel), MethodType.Getter)]
        public static class Need_CurLevel_Getter_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(Need __instance, ref float __result)
            {
                var pawn = TryGetPawnFromNeed(__instance);
                if (pawn?.def?.defName == TargetDefName && (__instance is Need_Rest || __instance is Need_Food))
                {
                    __result = 1f;
                }
            }
        }

        // 隐藏睡眠/饮食条（通打 Need 的绘制方法）
        [HarmonyPatch(typeof(Need), nameof(Need.DrawOnGUI))]
        public static class Need_DrawOnGUI_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Need __instance)
            {
                var pawn = TryGetPawnFromNeed(__instance);
                if (pawn?.def?.defName == TargetDefName && (__instance is Need_Rest || __instance is Need_Food))
                    return false; // 跳过原方法
                return true;
            }
        }

        // —— 工具函数 ——
        private static Pawn TryGetPawn(HediffSet hs)
        {
            try { if (hs.pawn != null) return hs.pawn; } catch { }
            var t = typeof(HediffSet);
            var f = AccessTools.Field(t, "pawn") ?? AccessTools.Field(t, "pawnInt") ?? AccessTools.Field(t, "_pawn");
            return f?.GetValue(hs) as Pawn;
        }

        private static Pawn TryGetPawnFromNeed(Need need)
        {
            try
            {
                var f = AccessTools.Field(need.GetType(), "pawn") ?? AccessTools.Field(typeof(Need), "pawn");
                if (f != null) return f.GetValue(need) as Pawn;
            }
            catch { }
            return null;
        }
    }
}
