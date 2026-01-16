using LudeonTK;
using PromotionLib.PrLibHediffComp;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PromotionLib.dev
{

    [StaticConstructorOnStartup]
    public static class VirusStrainTest
    {
        [DebugAction("PromotionLib", "检测一个物品是否有携带毒株", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void CheckThingVirus()
        {
            DebugTools.curTool = new DebugTool("点击一个物品进行检测", delegate
            {
                Map map = Find.CurrentMap;
                IntVec3 cell = UI.MouseCell();
                List<Thing> things = cell.GetThingList(map);
                if (things == null || things.Count == 0)
                {
                    Messages.Message("未点击到任何物品。", MessageTypeDefOf.RejectInput, false);
                    return;
                }

                // 选取“最上面”的那个（列表末尾通常是可见顶部）
                Thing thing = things[things.Count - 1];

                // 举例：读取你的 Comp 并提示
                VirusStrainComp comp = thing.TryGetComp<VirusStrainComp>();
                if (comp == null)
                {
                    Messages.Message("该物品没有 VirusStrainComp。", MessageTypeDefOf.NeutralEvent, false);
                    return;
                }

                Messages.Message("检测到物品:" + comp.GetVirusStrainDescription(), MessageTypeDefOf.NeutralEvent, false);
                // TODO: 在这里调用你的检测逻辑
            });
        }


        [DebugAction("PromotionLib", "给一个格子的物品的物品添加测试毒株", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void addThingVirus()
        {
            DebugTools.curTool = new DebugTool("点击一个物品进行添加测试毒株", delegate
            {
                Map map = Find.CurrentMap;
                IntVec3 cell = UI.MouseCell();
                List<Thing> things = cell.GetThingList(map);
                if (things == null || things.Count == 0)
                {
                    Messages.Message("未点击到任何物品。", MessageTypeDefOf.RejectInput, false);
                    return;
                }

                // 选取"最上面"的那个（列表末尾通常是可见顶部）
                Thing thing = things[things.Count - 1];

                // 检查物品是否有VirusStrainComp组件
                VirusStrainComp comp = thing.TryGetComp<VirusStrainComp>();
                if (comp == null)
                {
                    Messages.Message("该物品没有 VirusStrainComp。", MessageTypeDefOf.NeutralEvent, false);
                    return;
                }

                // 创建一个测试毒株
                VirusStrain testStrain = new VirusStrain();
                testStrain.StrainName = "测试毒株";
                testStrain.UniqueID = "TEST001";
                testStrain.Type = VirusCategory.PandemicVirus;
                testStrain.Infectivity = 75;
                testStrain.Pathogenicity = 60;
                testStrain.AntigenStrength = 45f;
                testStrain.AirSurvivability = 80;
                testStrain.SurfacePersistence = 70;
                testStrain.MutationRate = 15;
                testStrain.MinIncubationPeriod = 1;
                testStrain.MaxIncubationPeriod = 5;
                testStrain.Symptoms = new List<string>();
                testStrain.IsCultivated = true;
                testStrain.MinAdaptedTemperature = 15f;
                testStrain.MaxAdaptedTemperature = 35f;
                testStrain.TargetRace = new List<string> { "Human" };
                testStrain.FluidTransmission = true;
                testStrain.InfectionSeverity = 1.2f;
                HediffComp_VirusStrainContainer virusStrainContainer = new HediffComp_VirusStrainContainer();
                virusStrainContainer.virus = testStrain;
                // 将测试毒株添加到物品组件中
                comp.AddVirusStrain(virusStrainContainer);
            });
        }
    }
}
