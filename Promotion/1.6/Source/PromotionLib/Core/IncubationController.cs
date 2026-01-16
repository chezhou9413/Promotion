using PromotionLib.PrLibHediffComp;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class IncubationController : MapComponent
    {
        public IncubationController(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                if (!pawn.Spawned)
                    continue;

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                foreach (Hediff hediff in hediffs)
                {
                    HediffComp_VirusStrainContainer hediffComp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();

                    if (hediffComp != null && hediffComp.virus != null)
                    {
                        // 每tick减少潜伏期计时，限制最小为 -1
                        hediffComp.IncubationPeriodtick = Mathf.Max(-1, hediffComp.IncubationPeriodtick - 1);
                        
                        // 潜伏期结束触发一次性提示（在设置标志之前检查）
                        if (hediffComp.IncubationPeriodtick < 0 && hediffComp.IncubationPeriod == false && pawn.Faction != null && pawn.Faction == Faction.OfPlayer)
                        {
                            // 发送确诊通知
                            Find.LetterStack.ReceiveLetter(
                            "毒株确诊通知", // 信件标题
                            $"{pawn.LabelShortCap} 被确诊感染了毒株型号为 {hediffComp.virus.StrainName}（Ves: {hediffComp.virus.StrainVersion}）的症状，请注意观察与处理！", // 正文内容
                            LetterDefOf.ThreatSmall, // 警告等级（黄色 + 警告音效）
                            pawn                     // 跳转目标（点击信件后聚焦角色）
                            );
                            // 设置潜伏期结束标志，防止重复发送通知
                            hediffComp.IncubationPeriod = true;
                        }
                    }
                }
            }

        }
    }
}
