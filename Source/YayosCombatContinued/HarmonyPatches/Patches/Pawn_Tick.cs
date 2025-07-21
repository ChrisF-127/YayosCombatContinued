using HarmonyLib;
using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.Tick))]
	internal class Pawn_Tick
	{
		[HarmonyPriority(0)]
		private static void Postfix(Pawn __instance)
		{
			if (!YayosCombatContinued.Settings.UseAmmo
				|| __instance?.Drafted != true
				|| __instance.equipment == null
				|| Find.TickManager.TicksGame % 60 != 0)
				return;

			var job = __instance.CurJobDef;
			// if attacking at range, try reloading only primary once it runs out of ammo
			if (job == JobDefOf.AttackStatic)
				ReloadUtility.TryAutoReloadSingle(__instance.GetPrimary().TryGetComp<CompApparelReloadable>());
			// if waiting (drafted), try reloading all weapons that are out of ammo and for which ammo can be found
			else if (job == JobDefOf.Wait_Combat)
				ReloadUtility.TryAutoReloadAll(__instance);
		}
	}
}
