using PromotionLib.Utils;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PromotionLib.comp
{
    public class BuildingStrainClearCompProperties : CompProperties
    {
        /// <summary>
        /// 是否禁用空气传播
        /// </summary>
        public bool DisableAirborneTransmission = false;
        /// <summary>
        /// 是否禁用接触传播
        /// </summary>
        public bool isContactTransmissionEnabled = false;
        /// <summary>
        /// 是否禁用体液传播
        /// </summary>
        public bool isFluidTransmissionEnabled = false;
        /// <summary>
        /// 是否对范围内物品进行消毒
        /// </summary>
        public bool DisinfectItemsInRadius = false;
        /// <summary>
        /// 作用的范围效果
        /// </summary>
        public int effectRange = 20;
        /// <summary>
        /// 是否需要电力支持
        /// </summary>
        public bool requiresPower = false;
        /// <summary>
        /// 是否需要燃料支持
        /// </summary>
        public bool hasRes = true;
        /// <summary>
        /// 是否会被障碍阻挡
        /// </summary>
        public bool IsObstructed = false;
        public BuildingStrainClearCompProperties()
        {
            this.compClass = typeof(BuildingStrainClearComp);
        }
    }
    public class BuildingStrainClearComp : ThingComp
    {
        public BuildingStrainClearCompProperties Props => (BuildingStrainClearCompProperties)this.props;

        private List<IntVec3> cachedVisibleCells = null;

        private HashSet<Pawn> trackedPawns = new HashSet<Pawn>();

        private int tick = 0;

        public override void CompTick()
        {
            if (tick != 0)
            {
                tick--;
                return;
            }
            if (Props.requiresPower)
            {
                CompPowerTrader powerComp = this.parent.TryGetComp<CompPowerTrader>();
                if (powerComp != null)
                {
                    // PowerOn 是最终的判断属性，它综合了“是否连接到电网”和“电网是否有电”
                    if (!powerComp.PowerOn)
                    {
                        return;
                    }
                }
                else
                {
                    Log.Error(this.parent + " 需要电力支持，但没有找到 CompPowerTrader 组件。");
                }
            }
            if (Props.hasRes)
            {
                CompRefuelable refuelableComp = this.parent.TryGetComp<CompRefuelable>();
                if (refuelableComp != null)
                {
                    if (!refuelableComp.HasFuel)
                    {
                        return;
                    }
                }
                else
                {
                    Log.Error(this.parent + " 需要燃料支持，但没有找到 CompRefuelable 组件。");
                }
            }
            if (Props.IsObstructed)
            {
                this.cachedVisibleCells = new List<IntVec3>();
                // 1. 获取半径内的所有格子
                // GenRadial.RadialCellsAround(中心点, 半径, 是否使用圆形)
                IEnumerable<IntVec3> allCellsInRange = GenRadial.RadialCellsAround(this.parent.Position, this.Props.effectRange, true);

                // 2. 过滤出视线可达的格子
                foreach (IntVec3 cell in allCellsInRange)
                {
                    // GenSight.LineOfSight(起点, 终点, 地图, 是否跳过起点)
                    // 这个方法会模拟一条射线，如果中间没有障碍物则返回 true
                    if (GenSight.LineOfSight(this.parent.Position, cell, this.parent.Map, skipFirstCell: true))
                    {
                        cachedVisibleCells.Add(cell);
                    }
                }
            }
            else
            {
                this.cachedVisibleCells = GenRadial.RadialCellsAround(this.parent.Position, this.Props.effectRange, true).ToList();
            }
            if (cachedVisibleCells != null)
            {
                setPawnVirusDisableForObstructed();
                DisinfectItems();
            }
            tick = 120;
            base.CompTick();
        }
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();

            if (parent.Spawned)
            {
                if (Props.IsObstructed)
                {
                    if (cachedVisibleCells != null)
                    {
                        GenDraw.DrawFieldEdges(this.cachedVisibleCells, Color.green);
                    }
                }
                else
                {
                    // 1. 定义你想要的颜色
                    Color ringColor = Color.green;
                    GenDraw.DrawRadiusRing(this.parent.Position, Props.effectRange, ringColor);
                }
            }
        }
        public void DisinfectItems()
        {
            if (Props.DisinfectItemsInRadius) {
                HashSet<Thing> results = new HashSet<Thing>();
                // 遍历所有可见的格子
                // (注意: cachedVisibleCells 必须在别处被正确更新，例如 CompTickRare 或 PostDrawExtraSelectionOverlays)
                if (cachedVisibleCells != null)
                {
                    foreach (IntVec3 cell in cachedVisibleCells)
                    {
                        // 获取格子上的所有物品
                        List<Thing> thingsOnCell = this.parent.Map.thingGrid.ThingsListAt(cell);
                        if (thingsOnCell.Count > 0)
                        {
                            // 将它们全部加入到结果集中
                            foreach (Thing t in thingsOnCell)
                            {
                                results.Add(t);
                            }
                        }
                    }
                }
                foreach (Thing t in results)
                {
                    VirusStrainComp comp = t.TryGetComp<VirusStrainComp>();
                    if (comp != null && comp.VirusStrain.Count > 0)
                    {
                        comp.VirusStrain.Clear();
                    }
                }
            }
        }
        public void setPawnVirusDisableForObstructed()
        {
            // 1. 获取当前区域内的所有 Pawn，这部分逻辑和原来一样
            HashSet<Thing> results = new HashSet<Thing>();
            // 遍历所有可见的格子
            // (注意: cachedVisibleCells 必须在别处被正确更新，例如 CompTickRare 或 PostDrawExtraSelectionOverlays)
            if (cachedVisibleCells != null)
            {
                foreach (IntVec3 cell in cachedVisibleCells)
                {
                    // 获取格子上的所有物品
                    List<Thing> thingsOnCell = this.parent.Map.thingGrid.ThingsListAt(cell);
                    if (thingsOnCell.Count > 0)
                    {
                        // 将它们全部加入到结果集中
                        foreach (Thing t in thingsOnCell)
                        {
                            results.Add(t);
                        }
                    }
                }
            }
            // 从所有物品中筛选出 Pawn，得到“当前状态”
            HashSet<Pawn> currentPawnsInArea = results.OfType<Pawn>().ToHashSet();

            // 2. 找出【离开】的 Pawn
            // 他们存在于“旧的追踪列表(trackedPawns)”中，但不存在于“当前状态”中
            var pawnsThatLeft = trackedPawns.Except(currentPawnsInArea).ToList();
            foreach (var pawn in pawnsThatLeft)
            {

                setPawnVirusFalse(pawn);
            }

            // 3. 找出【新进入】的 Pawn
            // 他们存在于“当前状态”中，但不存在于“旧的追踪列表(trackedPawns)”中
            var pawnsThatEntered = currentPawnsInArea.Except(trackedPawns).ToList();
            foreach (var pawn in pawnsThatEntered)
            {

                setPawnVirus(pawn);
            }

            // 4. 最后，用“当前状态”更新追踪列表，为下一次检查做准备
            // 这一步必须在所有比较完成之后执行
            this.trackedPawns = currentPawnsInArea;
        }

        public void setPawnVirus(Pawn pawn)
        {
            if (Props.DisableAirborneTransmission)
            {
                List<HediffComp_VirusStrainContainer> hediffComps = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(pawn);
                foreach (HediffComp_VirusStrainContainer hediffComp in hediffComps)
                {
                    hediffComp.DisableAirborneTransmission = true;
                }
            }
            if (Props.isContactTransmissionEnabled)
            {
                List<HediffComp_VirusStrainContainer> hediffComps = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(pawn);
                foreach (HediffComp_VirusStrainContainer hediffComp in hediffComps)
                {
                    hediffComp.isContactTransmissionEnabled = true;
                }
            }
            if (Props.isFluidTransmissionEnabled)
            {
                List<HediffComp_VirusStrainContainer> hediffComps = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(pawn);
                foreach (HediffComp_VirusStrainContainer hediffComp in hediffComps)
                {
                    hediffComp.isFluidTransmissionEnabled = true;
                }
            }
        }

        public void setPawnVirusFalse(Pawn pawn)
        {
            List<HediffComp_VirusStrainContainer> hediffComps = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(pawn);
            foreach (HediffComp_VirusStrainContainer hediffComp in hediffComps)
            {
                hediffComp.DisableAirborneTransmission = false;
                hediffComp.isContactTransmissionEnabled = false;
                hediffComp.isFluidTransmissionEnabled = false;
            }
        }
    }
}
