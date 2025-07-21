using HarmonyLib;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(Tool), nameof(Tool.AdjustedCooldown), typeof(Thing))]
	internal class Tool_AdjustedCooldown
	{
		[HarmonyPriority(0)]
		private static void Postfix(ref float __result, Thing ownerEquipment)
		{
#warning Why "meleeRandom > 0f" ???
			var meleeRandom = YayosCombatContinued.Settings.MeleeRandom;
			if (meleeRandom > 0f)
				return;
			if (ownerEquipment == null)
				return;
			if (!(__result > 0f))
				return;
			if (!(ownerEquipment.ParentHolder is Pawn))
				return;
			if (!ownerEquipment.def.IsMeleeWeapon)
				return;

			var factor = YayosCombatContinued.Settings.MeleeDelay * (1f + ((Rand.Value - 0.5f) * meleeRandom));
			__result = Mathf.Max(__result * factor, 0.2f);
		}
	}
}
