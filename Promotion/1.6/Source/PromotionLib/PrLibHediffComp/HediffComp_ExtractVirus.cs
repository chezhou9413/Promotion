using PromotionLib.PrLibDef;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PromotionLib.PrLibHediffComp
{
    public class HediffCompProperties_ExtractVirus : HediffCompProperties
    {
        public ThingDef ExtractVirusThingDef;
        public ExtractVirusPool extractVirusPool;
        public HediffCompProperties_ExtractVirus()
        {
            this.compClass = typeof(HediffComp_ExtractVirus);
        }
    }
    public class HediffComp_ExtractVirus : HediffComp
    {
        public HediffCompProperties_ExtractVirus Props => (HediffCompProperties_ExtractVirus)this.props;
    }
}
