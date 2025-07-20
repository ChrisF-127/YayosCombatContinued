using RimWorld;
using Verse;

namespace YayosCombatContinued
{
    [DefOf]
	internal static class YCC_ThingCategoryDefOf
    {
        public static ThingCategoryDef YCC_AmmoCategory;

        static YCC_ThingCategoryDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(YCC_ThingCategoryDefOf));
        }
    }
}
