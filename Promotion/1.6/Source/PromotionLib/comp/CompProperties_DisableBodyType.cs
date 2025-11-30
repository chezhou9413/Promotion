using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class CompProperties_DisableBodyType : CompProperties
    {
        public CompProperties_DisableBodyType()
        {
            compClass = typeof(Comp_DisableBodyType);
        }
    }

    public class Comp_DisableBodyType : ThingComp { }

    /// <summary>
    /// 完全接管 ApparelGraphicRecordGetter.TryGetGraphicApparel：
    /// - 支持禁用体型后缀（Comp_DisableBodyType）
    /// - 更稳的资源探测：优先 Multi（方向齐全），否则 Single（基底或任一分向）
    /// - 在找不到时对“带/不带体型后缀”的路径做自动回退
    /// - 找不到任何资源时安全返回 false，避免 MatFrom null
    /// </summary>
    [HarmonyPatch(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel))]
    public static class Patch_TryGetGraphicApparel
    {
        public static bool Prefix(Apparel apparel, BodyTypeDef bodyType, bool forStatue, ref ApparelGraphicRecord rec, ref bool __result)
        {
            // 1) 路径基础校验
            if (apparel?.WornGraphicPath.NullOrEmpty() ?? true)
            {
                rec = new ApparelGraphicRecord(null, null);
                __result = false;
                return false; // 阻止原方法
            }

            // 2) 是否禁用体型后缀
            bool disableBodyType = apparel.TryGetComp<Comp_DisableBodyType>() != null;

            // 3) 计算初始 basePath（可能带体型后缀，具体取决于层级/是否背包/占位图等）
            string computedPath = ComputeBasePath(apparel, bodyType, disableBodyType);

            // 4) 生成候选路径（优先用 computedPath；如果是禁用体型但资源缺失，则回退尝试带体型；反之亦然）
            //    注意：候选列表要去重，避免重复探测。
            var candidates = new List<string>(2);
            candidates.Add(computedPath);

            string altPath = ComputeBasePath(apparel, bodyType, !disableBodyType);
            if (!string.Equals(altPath, computedPath))
                candidates.Add(altPath);

            // 5) Shader 选择（保持你原有逻辑）
            Shader shader = ShaderDatabase.Cutout;
            if (!forStatue)
            {
                if (apparel.StyleDef?.graphicData?.shaderType != null)
                {
                    shader = apparel.StyleDef.graphicData.shaderType.Shader;
                }
                else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) ||
                         (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
                {
                    shader = ShaderDatabase.CutoutComplex;
                }
            }

            // 6) 逐个候选路径进行资源探测与加载
            foreach (var basePath in candidates)
            {
                // 探测是否有基底贴图（不带方向后缀）
                bool hasBase = ContentFinder<Texture2D>.Get(basePath, false) != null;

                // 探测分向贴图
                bool hasSouth = ContentFinder<Texture2D>.Get(basePath + "_south", false) != null;
                bool hasNorth = ContentFinder<Texture2D>.Get(basePath + "_north", false) != null;
                bool hasEast = ContentFinder<Texture2D>.Get(basePath + "_east", false) != null;

                Graphic graphic = null;

                // 优先使用 Multi：至少 south + east 存在（或三向更好）
                if ((hasSouth && hasEast) || (hasSouth && hasNorth && hasEast))
                {
                    graphic = GraphicDatabase.Get<Graphic_Multi>(
                        basePath, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
                }
                else if (hasBase)
                {
                    // 有基底 → Single(basePath)
                    graphic = GraphicDatabase.Get<Graphic_Single>(
                        basePath, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
                }
                else if (hasSouth || hasEast || hasNorth)
                {
                    // 没有基底，但有任一分向 → 用已有的分向路径精确加载 Single
                    string fallback = hasSouth ? basePath + "_south" : (hasEast ? basePath + "_east" : basePath + "_north");
                    graphic = GraphicDatabase.Get<Graphic_Single>(
                        fallback, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
                }

                if (graphic != null)
                {
                    rec = new ApparelGraphicRecord(graphic, apparel);
                    __result = true;
                    return false; // 阻止原方法，成功
                }
            }

            // 7) 到这里说明所有候选都失败了 —— 安全返回 false，并给出一次性 Warning，便于定位问题
            Log.Warning($"[PromotionLib] Apparel graphic not found for '{apparel.LabelCap ?? apparel.def?.defName}'. " +
                        $"Tried paths: {string.Join(", ", candidates)} (base / _south / _east / _north). " +
                        $"Check bodyType suffix & resource files.");
            rec = new ApparelGraphicRecord(null, null);
            __result = false;
            return false;
        }

        /// <summary>
        /// 计算最终用于加载的 basePath（可能带体型后缀）。
        /// 逻辑与原版一致：只有在非头部/非眼部/非背包/非占位图时才考虑体型后缀。
        /// 当 disableBodyType = true 时强制不加体型后缀。
        /// </summary>
        private static string ComputeBasePath(Apparel apparel, BodyTypeDef bodyType, bool disableBodyType)
        {
            string basePath = apparel.WornGraphicPath;

            bool shouldAppendBodyType =
                !disableBodyType &&
                apparel.def?.apparel?.LastLayer != ApparelLayerDefOf.Overhead &&
                apparel.def?.apparel?.LastLayer != ApparelLayerDefOf.EyeCover &&
                !apparel.RenderAsPack() &&
                basePath != BaseContent.PlaceholderImagePath &&
                basePath != BaseContent.PlaceholderGearImagePath;

            if (shouldAppendBodyType && bodyType != null)
            {
                basePath = basePath + "_" + bodyType.defName;
            }

            return basePath;
        }
    }
}
