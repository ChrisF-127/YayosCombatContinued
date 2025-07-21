using HarmonyLib;
using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(WorkGiver_HunterHunt), nameof(WorkGiver_HunterHunt.HasHuntingWeapon))]
	internal class WorkGiver_HasHuntingWeapon
	{
		private static bool Postfix(bool __result, Pawn p)
		{
			if (!__result || !YayosCombatContinued.Settings.UseAmmo)
				return __result;

			var comp = p.equipment.Primary.TryGetComp<CompApparelReloadable>();
			if (comp != null)
				__result = comp.CanBeUsed(out _);

			return __result;
		}
	}
}
