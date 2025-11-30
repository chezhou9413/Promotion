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
            if (VirusStrain.Contains(newVirusStrain))
            {
                return;
            }
            VirusStrain.Add(newVirusStrain);
        }

        /// <summary>
        ///给物品毒株组件增加毒株集合
        /// </summary>
        public void AddVirusStrainList(List<HediffComp_VirusStrainContainer> newVirusStrain)
        {
            foreach (HediffComp_VirusStrainContainer strain in newVirusStrain)
            {
                if (VirusStrain.Contains(strain) || strain.isContactTransmissionEnabled)
                {
                    continue;
                }
                if (Rand.Value < (strain.virus.SurfacePersistence / 100f))
                {
                    VirusStrain.Add(strain);
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
            Scribe_Collections.Look(ref VirusStrain, "virusStrain");  // 将 VirusStrain 对象序列化
        }
    }
}
