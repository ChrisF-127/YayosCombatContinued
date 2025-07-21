using HarmonyLib;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
	public static class Thing_TakeDamage
	{
		private static void Prefix(ref DamageInfo damageInfo)
		{
#warning What is this meant to do!? Should it be "meleeDelay == 1f" -> return ???
			var meleeDelay = YayosCombatContinued.Settings.MeleeDelay;
			if (meleeDelay != 1f)
				return;
			if (damageInfo.Amount <= 0f)
				return;
			if (!damageInfo.Weapon.IsMeleeWeapon)
				return;

			damageInfo.SetAmount(Mathf.Max(1f, Mathf.RoundToInt(damageInfo.Amount * meleeDelay)));
		}
	}
}
