using HarmonyLib;
using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.SetFromPreset))]
	internal class ThingFilter_SetFromPreset
	{
		private static void Prefix(ThingFilter __instance, StorageSettingsPreset preset)
		{
			if (!YayosCombatContinued.Settings.UseAmmo)
				return;

			if (preset == StorageSettingsPreset.DefaultStockpile)
				__instance.SetAllow(YCC_ThingCategoryDefOf.YCC_AmmoCategory, true);
		}
	}
}
