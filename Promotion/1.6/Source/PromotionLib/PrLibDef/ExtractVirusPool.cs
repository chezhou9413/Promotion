using System.Collections.Generic;
using Verse;

namespace PromotionLib.PrLibDef
{
    public class ExtractVirusPool : Def
    {
        public Dictionary<ThingDef,float> ExtractViruslist = new Dictionary<ThingDef, float>();

        public ThingDef GetRandomVirus()
        {
            if (ExtractViruslist.EnumerableNullOrEmpty())
            {
                return null;
            }
            return ExtractViruslist.Keys.RandomElementByWeight(key => ExtractViruslist[key]);
        }
    }
}
