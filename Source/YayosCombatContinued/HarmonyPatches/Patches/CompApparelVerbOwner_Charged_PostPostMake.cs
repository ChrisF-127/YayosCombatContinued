using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
    [HarmonyPatch(typeof(CompApparelVerbOwner_Charged), nameof(CompApparelVerbOwner_Charged.PostPostMake))]
    internal class CompApparelVerbOwner_Charged_PostPostMake
    {
        [HarmonyPriority(0)]
        private static void Postfix(CompApparelVerbOwner_Charged __instance, ref int ___remainingCharges)
		{
			if (!YayosCombatContinued.Settings.UseAmmo
				|| !__instance.parent.def.IsWeapon)
				return;

			// keep weapons empty when generating them after the world has been generated
			if (GenTicks.TicksGame > 5)
                ___remainingCharges = 0;
			// and generate weapons with at most "max charges" when first generating the world
			else
				___remainingCharges = Mathf.Min(Mathf.RoundToInt(__instance.Props.maxCharges * YayosCombatContinued.Settings.AmmoAmountOnSpawn), __instance.Props.maxCharges);
        }
    }
}
