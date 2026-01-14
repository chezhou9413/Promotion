
using System.Collections.Generic;
using Verse;

namespace PromotionLib.PrLibThingComp
{
    public class ThingCompPorcupine_ExtractVirus : CompProperties
    {
        public List<HediffDef> virusHediffdef = new List<HediffDef>();
        public List<HediffDef> complicationCompHediffdef = new List<HediffDef>();
        public ThingCompPorcupine_ExtractVirus()
        {
            compClass = typeof(ThingComp_ExtractVirus);
        }

    }
    public class ThingComp_ExtractVirus:ThingComp
    {
        public ThingCompPorcupine_ExtractVirus Props => (ThingCompPorcupine_ExtractVirus)this.props;
    }
}
