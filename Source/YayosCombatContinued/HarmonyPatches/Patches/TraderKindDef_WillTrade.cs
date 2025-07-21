using HarmonyLib;
using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(TraderKindDef), nameof(TraderKindDef.WillTrade))]
	internal class TraderKindDef_WillTrade
	{
		private static bool Prefix(ref bool __result, TraderKindDef __instance, ThingDef td)
		{
			if (!YayosCombatContinued.Settings.UseAmmo)
				return true;

			if (__instance.defName == "Empire_Caravan_TributeCollector")
				return true;

			if (td.tradeTags?.Contains("Ammo") != true)
				return true;

			__result = true;
			return false;
		}
	}
}
