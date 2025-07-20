using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(ReloadableUtility), nameof(ReloadableUtility.FindSomeReloadableComponent))]
	internal class ReloadableUtility_FindSomeReloadableComponent
	{
		private static void Postfix(ref IReloadableComp __result, Pawn pawn, bool allowForcedReload)
		{
			if (!YayosCombatContinued.Settings.UseAmmo 
				|| __result != null)
				return;

			foreach (var thing in pawn.equipment.AllEquipmentListForReading)
			{
#warning TODO test which weapons are found via AllEquipmentListForReading & make sure whether SimpleSidearms compatibility is needed
				Log.Message($"ReloadableUtility_FindSomeReloadableComponent: AllEquipmentListForReading: {thing}");
				var comp = thing.TryGetComp<CompApparelReloadable>();
				if (comp?.NeedsReload(allowForcedReload) != true)
					continue;

				__result = comp;
				return;
			}

			if (YayosCombatContinued.SimpleSidearmsCompatibility)
			{
				foreach (var thing in pawn.GetSimpleSidearms())
				{
					Log.Message($"ReloadableUtility_FindSomeReloadableComponent: GetSimpleSidearms: {thing}");
					// requires secondary patch to JobDriver_Reload.MakeNewToils (must only fail if comp.Wearer is neither pawn nor comp.Parent is in pawn's inventory)
					var comp = thing.TryGetComp<CompApparelReloadable>();
					if (comp?.NeedsReload(allowForcedReload) != true)
						continue;

					if (comp.AmmoDef.AnyReservableReachableThing(pawn, comp.MinAmmoNeeded(allowForcedReload)))
					{
						__result = comp;
						return;
					}
				}
			}
		}
	}
}
