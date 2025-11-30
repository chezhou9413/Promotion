using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class ComplicationHediff : HediffWithComps
    {
        private bool messageSent = false;

        public override void Tick()
        {
            base.Tick();

            // 自动在 severity 降到 0.01 以下时移除
            if (this.Severity <= 0.01f)
            {
                pawn.health.RemoveHediff(this);
            }
        }
        public override string Label
        {
            get
            {
                var comp = this.TryGetComp<ComplicationComp>();
                if (comp != null)
                {
                    string compType = comp.Complication.ComplicationType;
                    string baseLabel = base.Label;
                    if (Prefs.DevMode)
                    {
                        if (compType == "GenericComplication" || compType == "SignatureComplication" || compType == "NeuroSignatureComplication")
                            return $"{baseLabel} 严重等级: {comp.Complication.severityLevel} 进展程度: {this.Severity}";
                        else if (compType == "AbilityComplication" || compType == "EvolutionComplication")
                            return $"{baseLabel} 强化等级: {comp.Complication.severityLevel}";
                    }
                    else
                    {
                        if (compType == "GenericComplication" || compType == "SignatureComplication" || compType == "NeuroSignatureComplication")
                            return $"{baseLabel} 严重等级: {comp.Complication.severityLevel}";
                        else if (compType == "AbilityComplication" || compType == "EvolutionComplication")
                            return $"{baseLabel} 强化等级: {comp.Complication.severityLevel}";
                    }
                }
                return base.Label;
            }
        }

        public override Color LabelColor
        {
            get
            {
                var comp = this.TryGetComp<ComplicationComp>();
                if (comp != null && comp.Complication != null)
                {
                    string type = comp.Complication.ComplicationType;

                    if (type == "GenericComplication") return new Color(0.6f, 0.6f, 0.6f);
                    if (type == "SignatureComplication") return new Color(0.8f, 0.1f, 0.1f);
                    if (type == "NeuroSignatureComplication") return new Color(0.6f, 0.3f, 1f);
                    if (type == "AbilityComplication") return new Color(0.2f, 0.6f, 1f);
                    if (type == "EvolutionComplication") return new Color(1f, 0.85f, 0.3f);
                }
                return base.LabelColor;
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            // 不再需要运行时修改 def.stages
        }

        public void sendmessage(string type)
        {
            if (messageSent) return;

            // 只通知玩家殖民地的小人
            if (pawn.Faction != Faction.OfPlayer) return;

            messageSent = true;

            string title = "症状通知";
            string body = "";
            LetterDef letterDef = LetterDefOf.NeutralEvent;

            if (type == "GenericComplication")
            {
                letterDef = LetterDefOf.NegativeEvent;
                body = $"{pawn.LabelShortCap} 被诊断为轻度并发症：{this.Label}，当前无需紧急干预。";
            }
            else if (type == "SignatureComplication")
            {
                letterDef = LetterDefOf.ThreatSmall;
                body = $"{pawn.LabelShortCap} 被确诊为明显症状：{this.Label}，需要密切观察和治疗。";
            }
            else if (type == "NeuroSignatureComplication")
            {
                letterDef = LetterDefOf.ThreatSmall;
                body = $"{pawn.LabelShortCap} 出现神经类并发症：{this.Label}，可能影响其精神与意识。";
            }
            else if (type == "AbilityComplication")
            {
                letterDef = LetterDefOf.PositiveEvent;
                body = $"{pawn.LabelShortCap} 的神经系统异常激活，表现出反常的能力提升（{this.Label}）。";
            }
            else if (type == "EvolutionComplication")
            {
                letterDef = LetterDefOf.PositiveEvent;
                body = $"{pawn.LabelShortCap} 身体机能得以升华（{this.Label}）。";
            }

            if (!body.NullOrEmpty())
            {
                Find.LetterStack.ReceiveLetter(title, body, letterDef, pawn);
            }
        }


        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            var comp = this.TryGetComp<ComplicationComp>();
            if (comp == null) return;

            string compType = comp.Complication.ComplicationType;
            sendmessage(compType);

            string targetScope = comp.Props.scope?.ToLowerInvariant();

            var existing = pawn.health.hediffSet.hediffs
                .Where(h => h.def == this.def && h != this)
                .ToList();

            if (targetScope == "wholebody")
            {
                if (existing.Any(h => h.Part == null))
                {
                    pawn.health.RemoveHediff(this);
                    return;
                }
                this.Part = null;
            }
            else if (targetScope == "bodypart")
            {
                var usedParts = existing.Select(h => h.Part).Where(p => p != null).ToHashSet();
                var availableParts = pawn.health.hediffSet.GetNotMissingParts()
                    .Where(part => !usedParts.Contains(part)).ToList();

                if (availableParts.Any())
                {
                    this.Part = availableParts.RandomElement();
                }
                else
                {
                    pawn.health.RemoveHediff(this);
                }
            }
        }

        public override bool TendableNow(bool ignoreTimer = false)
        {
            var comp = this.TryGetComp<ComplicationComp>();
            string compType = comp?.Complication?.ComplicationType;

            if (compType == "AbilityComplication" || compType == "EvolutionComplication")
                return false;

            // 其余情况全部走原版逻辑，保证冷却机制生效
            return base.TendableNow(ignoreTimer);
        }
    }
}
