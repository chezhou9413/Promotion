using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PromotionLib
{
    public class AirborneInfectionController : MapComponent
    {
        // 用来控制每个pawn的计时
        private Dictionary<Pawn, int> pawnAirborneInfectionTicks = new Dictionary<Pawn, int>();
        //创建感染的生物集合
        private List<Pawn> nearbyCreatures = new List<Pawn>();
        public AirborneInfectionController(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            // 遍历所有的pawn
            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                if (!pawn.Spawned)
                    continue;

                // 如果是新pawn, 初始化计时
                if (!pawnAirborneInfectionTicks.ContainsKey(pawn))
                {
                    pawnAirborneInfectionTicks[pawn] = 0;
                }

                // 每tick执行
                pawnAirborneInfectionTicks[pawn]++;

                // 每180 ticks执行一次
                if (pawnAirborneInfectionTicks[pawn] >= 180)
                {
                    // 执行空气传播传染期逻辑
                    HandleAirborneInfectionLogic(pawn);

                    // 重置计时
                    pawnAirborneInfectionTicks[pawn] = 0;
                }
            }
        }

        // 处理空气传播传染期的逻辑
        private void HandleAirborneInfectionLogic(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (Hediff h in hediffs)
            {
                HediffComp_VirusStrainContainer hediffComp = h.TryGetComp<HediffComp_VirusStrainContainer>();
                if (hediffComp != null && hediffComp.virus != null)
                {
                    // 如果病毒具有空气生存能力
                    if (hediffComp.virus.AirSurvivability > 0)
                    {
                        // 获取附近的生物
                        List<Pawn> nearbyCreatures = GetNearbyCreatures(pawn, hediffComp.virus.AirSurvivability / 5);
                        foreach (Pawn pawn1 in nearbyCreatures)
                        {
                            if(!InfectionUtility.CheckRaceInfectability(pawn1, hediffComp.virus)){
                                return;
                            }
                            // 按照传播力判断是否感染
                            if (Rand.Value < (hediffComp.virus.Infectivity / 100f))
                            {
                                if(hediffComp.DisableAirborneTransmission)
                                {
                                    continue;
                                }
                                float pop = pawn1.GetStatValue(ValueDef.Sealing_level)*0.2f;
                                if(Rand.Value < pop)
                                {
                                    continue;
                                }
                                if (InfectionUtility.IsInfectedWithVirus(pawn1, hediffComp.virus))
                                {
                                    InfectionUtility.ExecuteVirusTransmission(pawn1, hediffComp.virus);
                                }
                            }
                        }
                    }
                }
            }
        }



        public static List<Pawn> GetNearbyCreatures(Pawn targetPawn, float radius)
        {
            List<Pawn> nearbyCreatures = new List<Pawn>();

            // 获取目标小人所在位置的所有格子
            IEnumerable<IntVec3> nearbyCells = GenRadial.RadialCellsAround(targetPawn.Position, radius, true);

            // 遍历所有格子
            foreach (IntVec3 cell in nearbyCells)
            {
                // 确保格子有效并且在地图上
                if (cell.InBounds(targetPawn.Map))
                {
                    // 获取该格子中的所有物品和生物
                    List<Thing> thingsInCell = cell.GetThingList(targetPawn.Map);

                    // 遍历该格子中的所有物品和生物
                    foreach (Thing thing in thingsInCell)
                    {
                        // 只选择Pawn类型的生物（包括小人和动物）
                        if (thing is Pawn pawn && pawn != targetPawn)
                        {
                            // 将生物添加到结果列表
                            nearbyCreatures.Add(pawn);
                        }
                    }
                }
            }

            return nearbyCreatures;
        }
    }
}
