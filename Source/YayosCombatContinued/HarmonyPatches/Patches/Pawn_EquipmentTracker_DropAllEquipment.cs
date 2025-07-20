using HarmonyLib;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DropAllEquipment))]
	internal class Pawn_EquipmentTracker_DropAllEquipment
	{
		private static void Prefix(Pawn_EquipmentTracker __instance, ThingOwner<ThingWithComps> ___equipment)
		{
			if (!YayosCombatContinued.Settings.EjectAmmoOnDowned
				|| !YayosCombatContinued.Settings.UseAmmo
				|| __instance.pawn.Faction?.IsPlayer == true)
				return;

			foreach (var thing in ___equipment)
				ReloadUtility.TryThingEjectAmmoDirect(thing, true, __instance.pawn);
		}
	}
}
