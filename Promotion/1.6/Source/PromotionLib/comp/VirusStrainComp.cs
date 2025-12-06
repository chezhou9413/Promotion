using PromotionLib.PrLibHediffComp;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PromotionLib
{

    public class CompProperties_VirusStrain : CompProperties
    {
        public CompProperties_VirusStrain()
        {
            this.compClass = typeof(VirusStrainComp); // 设置 VirusStrainComp 作为组件
        }
    }
    public class VirusStrainComp : ThingComp
    {
        // 存储病毒毒株对象
        public List<HediffComp_VirusStrainContainer> VirusStrain = new List<HediffComp_VirusStrainContainer>();

        /// <summary>
        /// 初始化时检查并设置 VirusStrain 对象
        /// </summary>
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
        }

        /// <summary>
        ///给物品毒株组件增加毒株
        /// </summary>
        public void AddVirusStrain(HediffComp_VirusStrainContainer newVirusStrain)
        {
            if (newVirusStrain?.virus == null)
            {
                return;
            }
            // 去重：通过病毒 UniqueID 避免重复添加
            string uid = newVirusStrain.virus.UniqueID;
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (var existing in VirusStrain)
                {
                    if (existing?.virus != null && existing.virus.UniqueID == uid)
                    {
                        return;
                    }
                }
            }

            // 深拷贝：不直接保存外部 HediffComp 引用，创建一个新的容器并拷贝病毒
            var clonedContainer = new HediffComp_VirusStrainContainer
            {
                DisableAirborneTransmission = newVirusStrain.DisableAirborneTransmission,
                isContactTransmissionEnabled = newVirusStrain.isContactTransmissionEnabled,
                isFluidTransmissionEnabled = newVirusStrain.isFluidTransmissionEnabled,
            };
            clonedContainer.SetVirusDirectly(newVirusStrain.virus.Clone());
            VirusStrain.Add(clonedContainer);
        }

        /// <summary>
        ///给物品毒株组件增加毒株集合
        /// </summary>
        public void AddVirusStrainList(List<HediffComp_VirusStrainContainer> newVirusStrain)
        {
            if (newVirusStrain == null)
            {
                return;
            }
            foreach (HediffComp_VirusStrainContainer strain in newVirusStrain)
            {
                if (strain?.virus == null)
                {
                    continue;
                }
                if (strain.isContactTransmissionEnabled)
                {
                    continue;
                }
                // 以表面携带率作为附着概率
                if (Rand.Value < (strain.virus.SurfacePersistence / 100f))
                {
                    // 使用单个添加方法（内部会深拷贝并去重）
                    AddVirusStrain(strain);
                }
            }
        }

        /// <summary>
        /// 获取病毒毒株的描述信息（可以用于调试或展示）
        /// </summary>
        public string GetVirusStrainDescription()
        {
            string msg= "包含的病毒毒株：\n";
            foreach (HediffComp_VirusStrainContainer strain in VirusStrain)
            {
                msg += "毒株名称："+strain.virus.StrainName+" "+"毒株ID:"+strain.virus.UniqueID + "\n";
            }
            return msg;
        }

        /// <summary>
        /// 用于序列化 VirusStrain 数据，确保它在存档时被保存
        /// </summary>
        public override void PostExposeData()
        {
            base.PostExposeData();  // 调用父类的序列化逻辑
            Scribe_Collections.Look(ref VirusStrain, "virusStrain", LookMode.Deep);  // 将 VirusStrain 对象序列化，使用 Deep 模式
        }
    }
}
