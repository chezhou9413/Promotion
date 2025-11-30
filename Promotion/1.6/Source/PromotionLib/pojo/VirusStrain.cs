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
    public class VirusStrain : ILoadReferenceable
    {

        public VirusCategory Type { get; set; } = VirusCategory.PandemicVirus;
        public string StrainName { get; set; } = "未知毒株";

        /// <summary>
        /// 动物是否可以被感染
        /// </summary>
        public bool CanInfectAnimals { get; set; } = false;

        /// <summary>
        /// 传播力，值越高传播越快
        /// </summary>
        public int Infectivity { get; set; } = 50;

        /// <summary>
        /// 致病性，值越高危害越大
        /// </summary>
        public int Pathogenicity { get; set; } = 50;

        /// <summary>
        /// 抗原强度，影响免疫系统识别
        /// </summary>
        public float AntigenStrength { get; set; } = 50;

        /// <summary>
        /// 在空气中的生存能力，值越高存活时间越长
        /// </summary>
        public int AirSurvivability { get; set; } = 50;

        /// <summary>
        ///表面携带率，影响通过接触传播的概率
        /// </summary>
        public int SurfacePersistence { get; set; } = 50;

        /// <summary>
        /// 突变率，影响病毒演化的速度
        /// </summary>
        public int MutationRate { get; set; } = 10;

        /// <summary>
        /// 最短潜伏期（tick）
        /// </summary>
        public int MinIncubationPeriod { get; set; } = 0;

        /// <summary>
        /// 最长潜伏期（tick）
        /// </summary>
        public int MaxIncubationPeriod { get; set; } = 3;

        /// <summary>
        /// 症状列表，病毒引起的症状
        /// </summary>
        public List<string> Symptoms { get; set; } = new List<string>();

        /// <summary>
        /// 毒株的版本
        /// </summary>
        public int StrainVersion { get; set; } = 1;

        /// <summary>
        /// 是否是人工培养的病毒
        /// </summary>
        public bool IsCultivated { get; set; } = false;

        /// <summary>
        /// 适应的最小温度
        /// </summary>
        public float MinAdaptedTemperature { get; set; } = -10f;

        /// <summary>
        /// 适应的最大温度
        /// </summary>
        public float MaxAdaptedTemperature { get; set; } = 40f;

        /// <summary>
        /// 目标种族列表，表示哪些种族可能被感染
        /// </summary>
        public List<string> TargetRace { get; set; } = new List<string>();

        /// <summary>
        /// 特殊毒株基因，用于病毒的特性
        /// </summary>
        public List<string> SpecialStrainGene { get; set; } = new List<string>();

        /// <summary>
        /// 毒株基因的列表
        /// </summary>
        public List<string> StrainGene { get; set; } = new List<string>();

        /// <summary>
        /// 是否通过体液传播
        /// </summary>
        public bool FluidTransmission { get; set; } = false;

        /// <summary>
        /// 是否是机械病毒
        /// </summary>
        public bool IsMechVirus { get; set; } = false;

        /// <summary>
        /// 是否是僵尸病毒
        /// </summary>
        public bool IsZombieVirus { get; set; } = false;

        /// <summary>
        /// 是否产生正面效果
        /// </summary>
        public bool IsPositiveEffect { get; set; } = false;

        /// <summary>
        /// 是否有永久效果
        /// </summary>
        public bool IsPermanentEffect { get; set; } = false;

        /// <summary>
        /// 毒株的唯一标识符
        /// </summary>
        public string UniqueID { get; set; } = "000";

        /// <summary>
        /// 感染的严重程度
        /// </summary>
        public float InfectionSeverity { get; set; } = 1f;

        /// <summary>
        /// 是否已经被中和
        /// </summary>
        public bool IsNeutralized { get; set; } = false;

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
    }
}
