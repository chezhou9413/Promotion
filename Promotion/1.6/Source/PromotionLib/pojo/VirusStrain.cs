using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace PromotionLib
{

    /// <summary>
    /// 毒株的名称
    /// </summary>
    public enum VirusCategory
    {
        PandemicVirus,        // 流行病毒类
        SimplePapillomaVirus, // 单纯疱疹类
        NeurotropicVirus,     // 嗜神经病毒类
        ExoticDNAVirus,       // 异种DNA病毒
        FlashRNAVirus,        // 闪耀RNA病毒
        NanoBioweapon,        // 纳米级生物武器
        ZeroToxicStructure,   // 零号毒株结构体
        AscendedFireVirus     // 升华火种类
    }
    /// <summary>
    /// 表示一种病毒毒株的定义，包括基础属性、传播能力、适应性、标记与唯一ID等。
    /// 用于 RimWorld 的病毒模拟系统。
    /// </summary>
    public class VirusStrain : ILoadReferenceable, IExposable
    {

        public VirusCategory Type = VirusCategory.PandemicVirus;
        public string StrainName = "未知毒株";

        /// <summary>
        /// 动物是否可以被感染
        /// </summary>
        public bool CanInfectAnimals = false;

        /// <summary>
        /// 传播力，值越高传播越快
        /// </summary>
        public int Infectivity = 50;

        /// <summary>
        /// 致病性，值越高危害越大
        /// </summary>
        public int Pathogenicity = 50;

        /// <summary>
        /// 抗原强度，影响免疫系统识别
        /// </summary>
        public float AntigenStrength = 50;

        /// <summary>
        /// 在空气中的生存能力，值越高存活时间越长
        /// </summary>
        public int AirSurvivability = 50;

        /// <summary>
        ///表面携带率，影响通过接触传播的概率
        /// </summary>
        public int SurfacePersistence = 50;

        /// <summary>
        /// 突变率，影响病毒演化的速度
        /// </summary>
        public int MutationRate = 10;

        /// <summary>
        /// 最短潜伏期（tick）
        /// </summary>
        public int MinIncubationPeriod = 0;

        /// <summary>
        /// 最长潜伏期（tick）
        /// </summary>
        public int MaxIncubationPeriod = 3;

        /// <summary>
        /// 症状列表，病毒引起的症状
        /// </summary>
        public List<string> Symptoms = new List<string>();

        /// <summary>
        /// 毒株的版本
        /// </summary>
        public int StrainVersion = 1;

        /// <summary>
        /// 是否是人工培养的病毒
        /// </summary>
        public bool IsCultivated = false;

        /// <summary>
        /// 适应的最小温度
        /// </summary>
        public float MinAdaptedTemperature = -10f;

        /// <summary>
        /// 适应的最大温度
        /// </summary>
        public float MaxAdaptedTemperature = 40f;

        /// <summary>
        /// 目标种族列表，表示哪些种族可能被感染
        /// </summary>
        public List<string> TargetRace = new List<string>();

        /// <summary>
        /// 特殊毒株基因，用于病毒的特性
        /// </summary>
        public List<string> SpecialStrainGene = new List<string>();

        /// <summary>
        /// 毒株基因的列表
        /// </summary>
        public List<string> StrainGene = new List<string>();

        /// <summary>
        /// 是否通过体液传播
        /// </summary>
        public bool FluidTransmission = false;

        /// <summary>
        /// 是否是机械病毒
        /// </summary>
        public bool IsMechVirus = false;

        /// <summary>
        /// 是否是僵尸病毒
        /// </summary>
        public bool IsZombieVirus = false;

        /// <summary>
        /// 是否产生正面效果
        /// </summary>
        public bool IsPositiveEffect = false;

        /// <summary>
        /// 是否有永久效果
        /// </summary>
        public bool IsPermanentEffect = false;

        /// <summary>
        /// 毒株的唯一标识符
        /// </summary>
        public string UniqueID = "000";

        /// <summary>
        /// 感染的严重程度
        /// </summary>
        public float InfectionSeverity = 1f;

        /// <summary>
        /// 是否已经被中和
        /// </summary>
        public bool IsNeutralized = false;

        /// <summary>
        ///需要治疗等级
        /// </summary>
        public int NeedHealLeve = 0;
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public VirusStrain()
        {
        }

        /// <summary>
        /// 返回毒株的唯一加载ID，用于数据的唯一标识
        /// </summary>
        public string GetUniqueLoadID()
        {
            return UniqueID;  // 返回唯一标识符
        }

        /// <summary>
        /// 实现 IExposable 接口，用于序列化和反序列化病毒毒株数据
        /// </summary>
        public void ExposeData()
        {
            Scribe_Values.Look(ref Type, "type", VirusCategory.PandemicVirus);
            Scribe_Values.Look(ref StrainName, "strainName", "未知毒株");
            Scribe_Values.Look(ref CanInfectAnimals, "canInfectAnimals", false);
            Scribe_Values.Look(ref Infectivity, "infectivity", 50);
            Scribe_Values.Look(ref Pathogenicity, "pathogenicity", 50);
            Scribe_Values.Look(ref AntigenStrength, "antigenStrength", 50f);
            Scribe_Values.Look(ref AirSurvivability, "airSurvivability", 50);
            Scribe_Values.Look(ref SurfacePersistence, "surfacePersistence", 50);
            Scribe_Values.Look(ref MutationRate, "mutationRate", 10);
            Scribe_Values.Look(ref MinIncubationPeriod, "minIncubationPeriod", 0);
            Scribe_Values.Look(ref MaxIncubationPeriod, "maxIncubationPeriod", 3);
            Scribe_Collections.Look(ref Symptoms, "symptoms", LookMode.Value);
            Scribe_Values.Look(ref StrainVersion, "strainVersion", 1);
            Scribe_Values.Look(ref IsCultivated, "isCultivated", false);
            Scribe_Values.Look(ref MinAdaptedTemperature, "minAdaptedTemperature", -10f);
            Scribe_Values.Look(ref MaxAdaptedTemperature, "maxAdaptedTemperature", 40f);
            Scribe_Collections.Look(ref TargetRace, "targetRace", LookMode.Value);
            Scribe_Collections.Look(ref SpecialStrainGene, "specialStrainGene", LookMode.Value);
            Scribe_Collections.Look(ref StrainGene, "strainGene", LookMode.Value);
            Scribe_Values.Look(ref FluidTransmission, "fluidTransmission", false);
            Scribe_Values.Look(ref IsMechVirus, "isMechVirus", false);
            Scribe_Values.Look(ref IsZombieVirus, "isZombieVirus", false);
            Scribe_Values.Look(ref IsPositiveEffect, "isPositiveEffect", false);
            Scribe_Values.Look(ref IsPermanentEffect, "isPermanentEffect", false);
            Scribe_Values.Look(ref UniqueID, "uniqueID", "000");
            Scribe_Values.Look(ref InfectionSeverity, "infectionSeverity", 1f);
            Scribe_Values.Look(ref IsNeutralized, "isNeutralized", false);
            Scribe_Values.Look(ref NeedHealLeve, "NeedHealLeve", 0);
        }

        public VirusStrain Clone()
        {
            return new VirusStrain
            {
                NeedHealLeve = this.NeedHealLeve,
                Type = this.Type,
                StrainName = this.StrainName,
                CanInfectAnimals = this.CanInfectAnimals,
                Infectivity = this.Infectivity,
                Pathogenicity = this.Pathogenicity,
                AntigenStrength = this.AntigenStrength,
                AirSurvivability = this.AirSurvivability,
                SurfacePersistence = this.SurfacePersistence,
                MutationRate = this.MutationRate,
                MinIncubationPeriod = this.MinIncubationPeriod,
                MaxIncubationPeriod = this.MaxIncubationPeriod,
                Symptoms = new List<string>(this.Symptoms ?? new List<string>()),
                StrainVersion = this.StrainVersion,
                IsCultivated = this.IsCultivated,
                MinAdaptedTemperature = this.MinAdaptedTemperature,
                MaxAdaptedTemperature = this.MaxAdaptedTemperature,
                TargetRace = new List<string>(this.TargetRace ?? new List<string>()),
                SpecialStrainGene = new List<string>(this.SpecialStrainGene ?? new List<string>()),
                StrainGene = new List<string>(this.StrainGene ?? new List<string>()),
                FluidTransmission = this.FluidTransmission,
                IsMechVirus = this.IsMechVirus,
                IsZombieVirus = this.IsZombieVirus,
                IsPositiveEffect = this.IsPositiveEffect,
                IsPermanentEffect = this.IsPermanentEffect,
                UniqueID = this.UniqueID, // 如需实例唯一，可在外部重新分配
                InfectionSeverity = this.InfectionSeverity,
                IsNeutralized = this.IsNeutralized,
            };
        }
    }
}
