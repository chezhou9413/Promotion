using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PromotionLib.Utils
{
    public static class VirusStrainUtils
    {
        public static List<HediffComp_VirusStrainContainer> GetAllHediffComp_VirusStrainContainerForPawn(Pawn pawn)
        {
            List<HediffComp_VirusStrainContainer> virusStrains = new List<HediffComp_VirusStrainContainer>();
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return virusStrains; // 返回空列表
            }
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff == null) continue;
                HediffComp_VirusStrainContainer comp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (comp?.virus != null)
                {
                    virusStrains.Add(comp);
                }
            }
            return virusStrains.Distinct().ToList(); // 返回不重复的病毒株列表
        }
        public static List<VirusStrain> GetAllHediffComp_VirusStrainContainerListConverVirusStrain(List<HediffComp_VirusStrainContainer> hediffComp_VirusStrainContainers)
        {
            List<VirusStrain> virusStrains = new List<VirusStrain>();
            foreach (HediffComp_VirusStrainContainer hediffComp in hediffComp_VirusStrainContainers)
            {
                if (hediffComp?.virus != null)
                {
                    virusStrains.Add(hediffComp.virus);
                }
            }
            return virusStrains; // 返回不重复的病毒株列表
        }
        public static List<VirusStrain> GetAllVirusStrainsForPawn(Pawn pawn)
        {
            List<VirusStrain> virusStrains = new List<VirusStrain>();
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return virusStrains; // 返回空列表
            }
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff == null) continue;
                HediffComp_VirusStrainContainer comp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (comp?.virus != null)
                {
                    virusStrains.Add(comp.virus);
                }
            }
            return virusStrains.Distinct().ToList(); // 返回不重复的病毒株列表
        }

        public static List<VirusStrain> DeduplicateStrains(List<VirusStrain> viruses)
        {
            List<VirusStrain> uniqueStrains = viruses.GroupBy(p => p.UniqueID).Select(group => group.First()).ToList();
            return uniqueStrains;
        }
    }
}
