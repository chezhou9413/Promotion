using HarmonyLib;
using PromotionLib.PrLibDefOf;
using PromotionLib.PrLibHediffComp;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class AirborneInfectionController : MapComponent
    {
        private const int IntervalTicks = 180;           // 完整扫描周期
        private const int Slices = 6;                    // 将一次完整扫描切成 6 片
        private const int MaxInfectionsPerSourcePerSlice = 3; // 每个源 pawn 在一个切片内最多感染的数量
        private const float RadiusFactor = 0.2f; 
        private int sliceCursor = 0;                   

        // 复用的临时列表
        private static readonly List<Pawn> tmpNearby = new List<Pawn>();

        public AirborneInfectionController(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            int sliceInterval = IntervalTicks / Slices;
            if (Find.TickManager.TicksGame % sliceInterval != 0)
                return;
            var pawns = map.mapPawns.AllPawnsSpawned;
            int count = pawns.Count;
            if (count == 0)
                return;
            int batchSize = Mathf.Max(1, count / Slices);
            int start = sliceCursor * batchSize;
            int end = (sliceCursor == Slices - 1) ? count : Mathf.Min(count, start + batchSize);
            for (int i = start; i < end; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || !pawn.Spawned)
                    continue;
                HandleAirborneInfectionForSource(pawn);
            }
            sliceCursor = (sliceCursor + 1) % Slices;
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

                int infectedThisSlice = 0;
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
                            infectedThisSlice++;
                            if (infectedThisSlice >= MaxInfectionsPerSourcePerSlice)
                                break; // 控制单源在本切片内最多感染数量，避免尖峰负载
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
