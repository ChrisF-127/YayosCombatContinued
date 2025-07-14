using HarmonyLib;
using RimWorld;
using Verse;

namespace yayoCombat;

[HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.SetFromPreset))]
internal class patch_ThingFilter_SetFromPreset
{
    [HarmonyPrefix]
    private static bool Prefix(ThingFilter __instance, StorageSettingsPreset preset)
    {
        if (!yayoCombat.ammo)
        {
            return true;
        }

        if (preset == StorageSettingsPreset.DefaultStockpile)
        {
            __instance.SetAllow(ThingCategoryDef.Named("yy_ammo_category"), true);
        }

        return true;
    }
}