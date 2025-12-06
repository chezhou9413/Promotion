using PromotionLib.PrLibHediffComp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class riwbioHediff : HediffWithComps
    {
        public override string Label
        {
            get
            {
                var comp = this.TryGetComp<HediffComp_VirusStrainContainer>();
                if (Prefs.DevMode)
                {
                    return $"[毒株感染] {comp.virus.StrainName} {(comp.strainProgress * 100):F2} 潜伏期剩余时间：{comp.IncubationPeriodtick} 是否是潜伏期：{!comp.IncubationPeriod}";
                }
                else if (comp != null)
                {
                    return $"[毒株感染] {comp.virus.StrainName} {(comp.strainProgress * 100):F2}%";
                }

                return "未知病毒";
            }
        }

        public override bool Visible
        {
            get
            {
                // 获取你自定义的 comp（比如病毒进度容器）
                var comp = this.TryGetComp<HediffComp_VirusStrainContainer>();
                if (Prefs.DevMode)
                {
                    return true; // 开发者模式下显示所有组件
                }else if (comp != null)
                {
                    return comp.IncubationPeriodtick < 1;
                }

                // 没有组件时默认不显示
                return false;
            }
        }

    }
}
