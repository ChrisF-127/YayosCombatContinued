using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetFloatMenuOptions))]
	internal class ThingWithComps_GetFloatMenuOptions
	{
		[HarmonyPriority(0)]
		private static void Postfix(ref IEnumerable<FloatMenuOption> __result, ThingWithComps __instance, Pawn selPawn)
		{
			if (!YayosCombatContinued.Settings.UseAmmo)
				return;

			var comp = __instance.TryGetComp<CompApparelReloadable>();
			if (selPawn.IsColonist 
				&& comp?.AmmoDef != null
				&& !comp.Props.destroyOnEmpty 
				&& comp.EjectableAmmo() > 0)
			{
				__result = new List<FloatMenuOption>
				{
					new FloatMenuOption("eject_AmmoAmount".Translate(comp.RemainingCharges, comp.AmmoDef.LabelCap), cleanWeapon, MenuOptionPriority.High)
				};
			}

			void cleanWeapon()
			{
				ReloadUtility.EjectAmmo(selPawn, __instance);
			}
		}
	}
}
