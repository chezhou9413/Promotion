using System;
using System.Collections.Generic;
using Verse;

namespace PromotionLib.PrLibHediffComp
{
    public class HediffCompProperties_VirusStrainContainer : HediffCompProperties
    {
        public string type = "PromotionLib.VirusStrain";
        public string uniqueID = "unknown";
        public string strainName = "未命名毒株";
        public int infectivity = 50;
        public int pathogenicity = 50;
        public float antigenStrength = 50;
        public int airSurvivability = 50;
        public int surfacePersistence = 50;
        public int mutationRate = 10;
        public int minIncubationPeriod = 0;
        public int maxIncubationPeriod = 0;
        public int strainVersion = 1;
        public bool isCultivated = false;
        public float minAdaptedTemperature = -10f;
        public float maxAdaptedTemperature = 40f;
        public int needHealLeve = 0;
        public bool fluidTransmission = false;
        public bool isMechVirus = false;
        public bool isZombieVirus = false;
        public bool isPositiveEffect = false;
        public bool isPermanentEffect = false;
        public bool isNeutralized = false;
        public float infectionSeverity = 1f;
        public bool CanInfectAnimals = false;
        public List<string> symptoms = new List<string>();
        public List<string> targetRace = new List<string>();
        public List<string> strainGene = new List<string>();
        public List<string> specialStrainGene = new List<string>();

        public HediffCompProperties_VirusStrainContainer()
        {
            this.compClass = typeof(HediffComp_VirusStrainContainer);
        }
    }
    public class HediffComp_VirusStrainContainer : HediffComp
    {
        public VirusStrain virus;

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
        public HediffCompProperties_VirusStrainContainer Props => (HediffCompProperties_VirusStrainContainer)this.props;
        
        public float strainProgress = 0f;

        public bool IncubationPeriod = false;

        public float IncubationPeriodtick = 0f;
        
        // 添加标志来控制是否从 Props 初始化病毒
        private bool skipPropsInitialization = false;
        
        public override void CompPostMake()
        {
            base.CompPostMake();
            // 只有在没有跳过初始化标志时才从 Props 初始化
            if (!skipPropsInitialization)
            {
                InitVirusFromProps();
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Deep.Look(ref virus, "virus");
            Scribe_Values.Look(ref skipPropsInitialization, "skipPropsInitialization", false);
            Scribe_Values.Look(ref DisableAirborneTransmission, "DisableAirborneTransmission", false);
            Scribe_Values.Look(ref isContactTransmissionEnabled, "isContactTransmissionEnabled", false);
            Scribe_Values.Look(ref isFluidTransmissionEnabled, "isFluidTransmissionEnabled", false);
            Scribe_Values.Look(ref strainProgress, "strainProgress", 0f);
            Scribe_Values.Look(ref IncubationPeriodtick, "IncubationPeriodtick", 0f);
            Scribe_Values.Look(ref IncubationPeriod, "IncubationPeriod", false);
        }
        
        /// <summary>
        /// 直接设置病毒实例，跳过从 Props 的初始化
        /// </summary>
        /// <param name="virusInstance">要设置的病毒实例</param>
        public void SetVirusDirectly(VirusStrain virusInstance)
        {
            skipPropsInitialization = true;
            virus = virusInstance;
            VirusConstraints.VirusStrainApply(ref virus); 
            if (virus != null)
            {
                strainProgress = virus.InfectionSeverity;
                IncubationPeriodtick = Rand.Range((int)virus.MinIncubationPeriod, (int)virus.MaxIncubationPeriod + 1);
            }
        }
        private void InitVirusFromProps()
        {
            try
            {
                // 创建病毒实例
                if (virus == null)
                {
                    virus = new VirusStrain();
                }
                if (Enum.TryParse(Props.type, true, out VirusCategory result))
                {
                    virus.Type = result;
                }
                else
                {
                    // 解析失败时的处理
                    virus.Type = VirusCategory.PandemicVirus; // 默认值
                }
                virus.NeedHealLeve = Props.needHealLeve;
                virus.UniqueID = Props.uniqueID;
                virus.StrainName = Props.strainName;
                virus.Infectivity = Props.infectivity;
                virus.Pathogenicity = Props.pathogenicity;
                virus.AntigenStrength = Props.antigenStrength;
                virus.AirSurvivability = Props.airSurvivability;
                virus.SurfacePersistence = Props.surfacePersistence;
                virus.MutationRate = Props.mutationRate;
                virus.MinIncubationPeriod = Props.minIncubationPeriod;
                virus.MaxIncubationPeriod = Props.maxIncubationPeriod;
                virus.StrainVersion = Props.strainVersion;
                virus.IsCultivated = Props.isCultivated;
                virus.MinAdaptedTemperature = Props.minAdaptedTemperature;
                virus.MaxAdaptedTemperature = Props.maxAdaptedTemperature;
                virus.FluidTransmission = Props.fluidTransmission;
                virus.IsMechVirus = Props.isMechVirus;
                virus.IsZombieVirus = Props.isZombieVirus;
                virus.IsPositiveEffect = Props.isPositiveEffect;
                virus.IsPermanentEffect = Props.isPermanentEffect;
                virus.InfectionSeverity = Props.infectionSeverity;
                virus.IsNeutralized = Props.isNeutralized;
                virus.CanInfectAnimals = Props.CanInfectAnimals;
                virus.Symptoms = new System.Collections.Generic.List<string>(Props.symptoms);
                virus.TargetRace = new System.Collections.Generic.List<string>(Props.targetRace);
                virus.StrainGene = new System.Collections.Generic.List<string>(Props.strainGene);
                virus.SpecialStrainGene = new System.Collections.Generic.List<string>(Props.specialStrainGene);
                VirusConstraints.VirusStrainApply(ref virus);
                strainProgress = virus.InfectionSeverity; // 初始感染进度为感染严重度的20倍
                IncubationPeriodtick = Rand.Range((int)virus.MinIncubationPeriod, (int)virus.MaxIncubationPeriod + 1); // 包含0，不包含100
            }
            catch (Exception ex)
            {
                Log.Error($"[VirusSystem] 病毒创建失败：{ex}");
            }
        }
    }
}
