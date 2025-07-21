using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(ReloadableUtility), nameof(ReloadableUtility.OwnerOf))]
	internal class ReloadableUtility_OwnerOf
	{
		private static void Postfix(ref Pawn __result, IReloadableComp reloadable)
		{
			if (!YayosCombatContinued.Settings.UseAmmo || __result != null)
				return;

			if (reloadable is CompApparelReloadable comp 
				&& comp.ParentHolder is Pawn_EquipmentTracker equipmentTracker 
				&& equipmentTracker.pawn != null)
				__result = equipmentTracker.pawn;
		}
	}
}
