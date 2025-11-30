using RimWorld;
using Verse;

namespace PromotionLib
{
    
    [DefOf]
    public static class ValueDef
    {
        // 你的 Def 声明（这部分是正确的）
        public static StatDef Disinfection_level;
        public static StatDef Sealing_level;

        // 静态构造函数（这部分也是正确的）
        static ValueDef()
        {
        }
    }
}