using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
	internal class Command_ReloadActions : Command_Action
	{
		private readonly Pawn Pawn = null;
		private readonly IEnumerable<Thing> ReloadableThings;

		public Command_ReloadActions(Pawn pawn)
		{
			defaultLabel = "YCC.ReloadGizmo_title".Translate();
			defaultDesc = "YCC.ReloadGizmo_desc".Translate();
			icon = Textures.AmmoReload;

			Pawn = pawn;
			ReloadableThings = pawn.GetAllReloadableThings();

			action = () => ReloadUtility.TryReloadFromInventory(pawn, ReloadableThings, true);
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
		{
			get
			{
				string inventory_label, inventory_tooltip;
				string surrounding_label, surrounding_tooltip;

				if (YayosCombatContinued.SimpleSidearmsCompatibility)
				{
					inventory_label = "YCC.ReloadAllWeaponFromInventory_label";
					inventory_tooltip = "YCC.ReloadAllWeaponFromInventory_tooltip";

					surrounding_label = "YCC.ReloadAllWeaponFromSurrounding_label";
					surrounding_tooltip = "YCC.ReloadAllWeaponFromSurrounding_tooltip";
				}
				else
				{
					inventory_label = "YCC.ReloadWeaponFromInventory_label";
					inventory_tooltip = "YCC.ReloadWeaponFromInventory_tooltip";

					surrounding_label = "YCC.ReloadWeaponFromSurrounding_label";
					surrounding_tooltip = "YCC.ReloadWeaponFromSurrounding_tooltip";
				}

				yield return new FloatMenuOption(
					inventory_label.Translate(),
					() => ReloadUtility.TryReloadFromInventory(Pawn, ReloadableThings, true))
				{
					tooltip = inventory_tooltip.Translate(),
				};
				yield return new FloatMenuOption(
					surrounding_label.Translate(),
					() => ReloadUtility.TryReloadFromSurrounding(Pawn, ReloadableThings, true, true))
				{
					tooltip = surrounding_tooltip.Translate(),
				};

				yield return new FloatMenuOption(
					"YCC.RestockAmmoFromSurrounding_label".Translate(),
					() => InventoryUtility.RestockInventoryFromSurrounding(Pawn)) 
				{ 
					tooltip = "YCC.RestockAmmoFromSurrounding_tooltip".Translate(), 
				};
			}
		}
	}
}
