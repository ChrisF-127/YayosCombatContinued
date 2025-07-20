using HarmonyLib;
using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(CompApparelReloadable), nameof(CompApparelReloadable.UsedOnce))]
	internal class CompApparelReloadable_UsedOnce
	{
		private static void Postfix(CompApparelReloadable __instance)
		{
			if (!yayoCombat.yayoCombat.ammo 
				|| __instance.Wearer == null)
				return;

			// (new) don't try to reload ammo that's not part of Yayo's Combat
			if (__instance.AmmoDef?.IsAmmo() != true)
				return;

			// (replacement) Replaced with new method
			var pawn = __instance.Wearer;
			var drafted = pawn.Drafted;
			if (!ReloadUtility.TryAutoReloadSingle(
					__instance,
					showOutOfAmmoWarning: true,
					ignoreDistance: !drafted,
					returnToStartingPosition: drafted)
				&& pawn.CurJobDef == JobDefOf.Hunt)
				pawn.jobs.StopAll();
		}
	}
}
