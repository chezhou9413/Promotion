using HarmonyLib;
using PromotionLib.PrLibHediffComp;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class AirborneInfectionController : MapComponent
    {
        private const float RadiusFactor = 0.2f; 

        // 复用的临时列表
        private static readonly List<Pawn> tmpNearby = new List<Pawn>();

        public AirborneInfectionController(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            int count = pawns.Count;
            if (count == 0)
                return;
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || !pawn.Spawned)
                    continue;
                HandleAirborneInfectionForSource(pawn);
            }
        }
        private void HandleAirborneInfectionForSource(Pawn source)
        {
            var hediffs = source.health?.hediffSet?.hediffs;
            if (hediffs == null || hediffs.Count == 0)
                return;

            foreach (Hediff h in hediffs)
            {
                if (h == null) continue;

                var comp = h.TryGetComp<HediffComp_VirusStrainContainer>();
                if (comp == null || comp.virus == null)
                    continue;

                // 禁用空气传播或无空气存活力则跳过
                if (comp.DisableAirborneTransmission || comp.virus.AirSurvivability <= 0)
                    continue;

                // 计算影响半径并收集邻近pawn
                float radius = Mathf.Max(1f, comp.virus.AirSurvivability * RadiusFactor);
                tmpNearby.Clear();
                FillNearbyCreatures(source, radius, tmpNearby);

                float baseProb = Mathf.Clamp01(comp.virus.Infectivity / 100f);

                foreach (Pawn target in tmpNearby)
                {
                    if (target == null || !target.Spawned || target.Dead)
                        continue;

                    // 过滤不可感染种族
                    if (!InfectionUtility.CheckRaceInfectability(target, comp.virus))
                        continue;

                    // 密封/防护抵抗（0~1）
                    float resist = Mathf.Clamp01(target.GetStatValue(ValueDef.Sealing_level) * 0.2f);
                    if (Rand.Value < resist)
                        continue;

                    // 距离衰减
                    float dist = (source.Position - target.Position).LengthHorizontal;
                    float falloff = Mathf.Clamp01(1f - (dist / (radius + 0.0001f)));

                    float finalProb = Mathf.Clamp01(baseProb * falloff);
                    if (finalProb <= 0f)
                        continue;

                    if (Rand.Value < finalProb)
                    {
                        // 未感染该病毒则执行传播
                        if (InfectionUtility.IsInfectedWithVirus(target, comp.virus))
                        {
                            InfectionUtility.ExecuteVirusTransmission(target, comp.virus);
                        }
                    }
                }
            }
        }

        // 将附近 Pawn 填充到 result（避免每次 new 列表）
        public static void FillNearbyCreatures(Pawn center, float radius, List<Pawn> result)
        {
            if (center == null || !center.Spawned)
                return;

            // 遍历半径内格子，收集 Pawn（不包含自身）
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center.Position, radius, useCenter: true))
            {
                if (!cell.InBounds(center.Map)) continue;

                List<Thing> things = cell.GetThingList(center.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn p && p != center)
                    {
                        result.Add(p);
                    }
                }
            }
        }
    }
}
