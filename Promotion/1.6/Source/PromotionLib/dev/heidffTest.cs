using LudeonTK;
using RimWorld;
using Verse;

namespace PromotionLib
{
    [StaticConstructorOnStartup]
    public static class heidffTest
    {
        /// <summary>
        /// Dev 模式按钮：添加五种并发症
        /// </summary>
        [DebugAction("PromotionLib", "添加全部测试并发症", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddAllComplicationsToPawn()
        {
            // 注册一个等待点击后的回调
            DebugTools.curTool = new DebugTool("点击一个角色添加并发症", delegate
            {
                // 获取点击位置的实体
                Pawn pawn = UI.MouseCell().GetFirstPawn(Find.CurrentMap);
                if (pawn == null)
                {
                    Messages.Message("未点击到任何 Pawn。", MessageTypeDefOf.RejectInput, false);
                    return;
                }

                ApplyAllTestComplications(pawn);
                Messages.Message($"已为 {pawn.LabelShort} 添加全部测试并发症。", pawn, MessageTypeDefOf.PositiveEvent);
            });
        }


        /// <summary>
        /// 实际逻辑：将五种测试用并发症添加到 Pawn
        /// </summary>
        private static void ApplyAllTestComplications(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned) return;

            string[] defNames = new string[]
            {
                "RimBio_Complication_Fatigue",
                "RimBio_Complication_SkinRash",
                "RimBio_Complication_CognitiveDecline",
                "RimBio_Complication_ReflexBoost",
                "RimBio_Complication_BodyReinforcement"
            };

            foreach (string defName in defNames)
            {
                HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    Log.Warning($"[RimBio Debug] 未找到 HediffDef：{defName}");
                    continue;
                }

                // 避免重复添加
                if (pawn.health.hediffSet.HasHediff(def)) continue;

                Hediff hediff = HediffMaker.MakeHediff(def, pawn);
                hediff.Severity = 0.1f;
                pawn.health.AddHediff(hediff);
            }

            Log.Message($"[RimBio Debug] 添加测试并发症至 {pawn.LabelShort} 完成。");
        }
    }
}
