using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PromotionLib
{
    public class ComplicationCompProperties : HediffCompProperties
    {
        public string type;
        public string scope;
        public int severityLevel = 1;

        public ComplicationCompProperties()
        {
            this.compClass = typeof(ComplicationComp);
        }
    }

    public class ComplicationComp : HediffComp
    {
        public Complication Complication { get; private set; }

        public ComplicationCompProperties Props => (ComplicationCompProperties)this.props;

        public override void CompPostMake()
        {
            base.CompPostMake();

            Complication = new Complication
            {
                ComplicationType = Props.type,
                TargetScope = Props.scope,
                severityLevel = Props.severityLevel
            };
        }

        public override void CompExposeData()
        {
            base.CompExposeData();

            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Values.Look(ref Complication.severityLevel, "severityLevel", 1);
            }
        }
    }
}
