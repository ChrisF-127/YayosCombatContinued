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
				var comp = thing.TryGetComp<CompApparelReloadable>();
				if (comp?.NeedsReload(allowForcedReload) != true)
					continue;

				__result = comp;
				return;
			}

			if (YayosCombatContinued.SimpleSidearmsCompatibility)
			{
				// requires Prefix to JobDriver_Reload.MakeNewToils which switches to thing that shall be reloaded (s. HarmonyPatches)
				//  fails otherwise since things in inventory cannot be reloaded
				foreach (var thing in pawn.GetSimpleSidearms())
				{
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
