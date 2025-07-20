using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GenerateGearFor))]
	internal class PawnGenerator_GenerateGearFor
	{
		[HarmonyPriority(Priority.Last)]
		private static void Postfix(Pawn pawn)
		{
			if (!YayosCombatContinued.Settings.UseAmmo
				|| pawn == null)
				return;

			var allWeaponsComps = new List<CompApparelReloadable>();
			// get all equipped weapons
			if (pawn.equipment?.AllEquipmentListForReading != null)
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				{
					if (!thing.def.IsWeapon)
						continue;

					var comp = thing.GetComp<CompApparelReloadable>();
					if (comp != null)
						allWeaponsComps.Add(comp);
				}
			}

			// get all weapons in inventory
			if (pawn.inventory?.innerContainer != null)
			{
				foreach (var thing in pawn.inventory.innerContainer)
				{
					if (!thing.def.IsWeapon)
						continue;

					var comp = thing.TryGetComp<CompApparelReloadable>();
					if (comp != null)
						allWeaponsComps.Add(comp);
				}
			}

			// end if no weapons found
			if (allWeaponsComps.Count == 0)
				return;

			var ammoToInventoryDict = new Dictionary<ThingDef, int>();
			// add ammo to equipped/carried weapons
			foreach (var comp in allWeaponsComps)
			{
				var ammoToGenerate = Mathf.RoundToInt(comp.MaxCharges * YayosCombatContinued.Settings.AmmoAmountOnSpawn * Rand.Range(0.7f, 1.3f));
				var charges = Mathf.Min(ammoToGenerate, comp.MaxCharges);
				comp.remainingCharges = charges;

				var remainder = ammoToGenerate - charges;
				if (remainder > 0)
					ammoToInventoryDict.IncreaseOrAdd(comp.AmmoDef, remainder);
			}
			// add additional ammo to inventory
			//if (pawn.Faction?.IsPlayer != true) // <- only for non-player faction?
			{
				foreach (var item in ammoToInventoryDict)
				{
					var ammoThing = ThingMaker.MakeThing(item.Key);
					ammoThing.stackCount = item.Value;
					pawn.inventory.innerContainer.TryAdd(ammoThing, item.Value);
				}
			}
		}
	}
}
