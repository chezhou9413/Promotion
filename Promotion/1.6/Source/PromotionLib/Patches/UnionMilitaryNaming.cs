using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;

namespace PromotionLib
{
    /// <summary>
    /// 联盟军方命名系统
    /// 负责生成派系名称和定居点名称
    /// </summary>
    public static class UnionMilitaryNaming
    {
        // 舰队类型
        private static readonly string[] FleetTypes = new string[]
        {
            "远征", "征服", "合作", "开拓"
        };

        // 代号库
        private static readonly string[] CodeNames = new string[]
        {
            "铁幕", "深空獠牙", "福音信使", "熵烬", "净世之光", "猩红黎明", 
            "不朽权杖", "锈火", "灰烬先驱", "沉默哨兵", "赤冠", "星尘", 
            "远疆", "铁律", "净焰", "幽影", "誓约", "歧路", "余烬", 
            "天秤", "断矛", "潮汐", "歧光", "黑曜", "归尘"
        };

        // 随机数生成器
        private static readonly Random Random = new Random();

        /// <summary>
        /// 生成派系名称
        /// 格式：第X号[类型]舰队
        /// </summary>
        public static string GenerateFactionName()
        {
            int fleetNumber = Random.Next(1, 100); // 1-99号
            string fleetType = FleetTypes[Random.Next(FleetTypes.Length)];
            
            return $"第{fleetNumber}号{fleetType}舰队";
        }

        /// <summary>
        /// 生成定居点名称
        /// 格式：第X号[类型]舰队"[代号]"
        /// </summary>
        public static string GenerateSettlementName()
        {
            int fleetNumber = Random.Next(1, 100); // 1-99号
            string fleetType = FleetTypes[Random.Next(FleetTypes.Length)];
            string codeName = CodeNames[Random.Next(CodeNames.Length)];
            
            return $"第{fleetNumber}号{fleetType}舰队\"{codeName}\"";
        }

        /// <summary>
        /// 获取随机代号
        /// </summary>
        public static string GetRandomCodeName()
        {
            return CodeNames[Random.Next(CodeNames.Length)];
        }

        /// <summary>
        /// 获取随机舰队类型
        /// </summary>
        public static string GetRandomFleetType()
        {
            return FleetTypes[Random.Next(FleetTypes.Length)];
        }
    }

    /// <summary>
    /// 联盟军方派系命名器补丁
    /// 在派系创建完成后设置名称
    /// </summary>
    [HarmonyPatch(typeof(Faction), "Name", MethodType.Getter)]
    public static class UnionMilitaryFactionNamingPatch
    {
        private static readonly Dictionary<Faction, string> FactionNames = new Dictionary<Faction, string>();

        [HarmonyPostfix]
        public static void Postfix(Faction __instance, ref string __result)
        {
            // 检查是否为联盟远征军派系
            if (__instance.def?.defName == "UnionExpedition")
            {
                // 如果已经生成过名称，直接返回
                if (FactionNames.ContainsKey(__instance))
                {
                    __result = FactionNames[__instance];
                    return;
                }

                // 生成新的派系名称
                string newName = UnionMilitaryNaming.GenerateFactionName();
                FactionNames[__instance] = newName;
                __result = newName;
            }
        }
    }

    /// <summary>
    /// 联盟军方定居点命名器补丁
    /// 使用更通用的方式处理定居点命名
    /// </summary>
    [HarmonyPatch(typeof(Thing), "Label", MethodType.Getter)]
    public static class UnionMilitarySettlementNamingPatch
    {
        private static readonly Dictionary<Thing, string> SettlementNames = new Dictionary<Thing, string>();

        [HarmonyPostfix]
        public static void Postfix(Thing __instance, ref string __result)
        {
            // 检查是否为联盟远征军派系的定居点
            if (__instance.Faction?.def?.defName == "UnionExpedition")
            {
                // 检查是否为定居点类型
                if (__instance.def.defName.Contains("Settlement") || __instance.def.defName.Contains("Base"))
                {
                    // 如果已经生成过名称，直接返回
                    if (SettlementNames.ContainsKey(__instance))
                    {
                        __result = SettlementNames[__instance];
                        return;
                    }

                    // 生成新的定居点名称
                    string newName = UnionMilitaryNaming.GenerateSettlementName();
                    SettlementNames[__instance] = newName;
                    __result = newName;
                }
            }
        }
    }

    
} 