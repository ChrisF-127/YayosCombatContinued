using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YayosCombatContinued
{
	[StaticConstructorOnStartup]
	public static class HarmonyPatches
	{
		private class JobInfo
		{
			public JobDef Def;
			public JobCondition EndCondition;
			public ThingWithComps PreviousWeapon;
		}
		private static readonly ConditionalWeakTable<Pawn, JobInfo> PawnPreviousJobTable = new ConditionalWeakTable<Pawn, JobInfo>();

		static HarmonyPatches()
		{
			var harmony = new Harmony("syrus.yayoscombatcontinued");

			// apply annotated patches
			harmony.PatchAll();

			// patch for reload gizmo
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_DraftController), nameof(Pawn_DraftController.GetGizmos)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_DraftController_GetGizmos_Postfix)));
			// patch for eject ammo gizmo
			harmony.Patch(
				AccessTools.Method(typeof(ThingComp), nameof(ThingComp.CompGetGizmosExtra)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ThingComp_CompGetGizmosExtra_Postfix)));

			// patch to reduce ammo in dropped weapons
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DropAllEquipment)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_EquipmentTracker_DropAllEquipment_Prefix)));
			// patch to reduce dropped ammo amount
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_InventoryTracker), nameof(Pawn_InventoryTracker.DropAllNearPawn)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_InventoryTracker_DropAllNearPawn_Prefix)));

			// patches to prevent reloading after hunting job fails (times out after 2h), stops pawns from going back and forth between hunting and reloading
			harmony.Patch(
				AccessTools.Method(typeof(JobGiver_Reload), nameof(JobGiver_Reload.GetPriority)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(JobGiver_Reload_GetPriority_Postfix)));
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_JobTracker_EndCurrentJob_Prefix)));

			// SimpleSidearms compatibility patches
			if (YayosCombatContinued.SimpleSidearmsCompatibility)
			{
				// patch to equip thing from inventory so it can be reloaded -- see JobDriver_Reload_MakeNewToils for Postfix
				harmony.Patch(
					AccessTools.Method(typeof(JobDriver_Reload), nameof(JobDriver_Reload.MakeNewToils)),
					prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(JobDriver_Reload_MakeNewToils_Prefix)));
			}
		}


		private static IEnumerable<Gizmo> Pawn_DraftController_GetGizmos_Postfix(IEnumerable<Gizmo> __result, Pawn_DraftController __instance)
		{
			var addReloadGizmo = false;

			var pawn = __instance?.pawn;
			if (yayoCombat.yayoCombat.ammo
				&& pawn != null
				&& pawn.Faction?.IsPlayer == true
				&& pawn.Drafted
				&& !pawn.WorkTagIsDisabled(WorkTags.Violent))
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
				{
					if (thing.TryGetComp<CompApparelReloadable>() != null)
					{
						addReloadGizmo = true;
						break;
					}
				}
			}

			foreach (var gizmo in __result)
			{
				yield return gizmo;

				if (addReloadGizmo
					&& gizmo is Command command
					&& command.tutorTag == "FireAtWillToggle")
				{
					yield return new Command_ReloadActions(pawn);
					addReloadGizmo = false;
				}
			}

			if (addReloadGizmo)
				Log.ErrorOnce($"{nameof(YayosCombatContinued)}: Failed to add 'Reload' gizmo!", 0x18051848);
		}

		private static IEnumerable<Gizmo> ThingComp_CompGetGizmosExtra_Postfix(IEnumerable<Gizmo> __result, ThingComp __instance)
		{
			if (__instance is CompApparelReloadable reloadable
				&& reloadable.AmmoDef.IsAmmo()
				&& (reloadable.Props.ammoCountToRefill > 0
					|| reloadable.Props.ammoCountPerCharge > 0))
			{
				var thing = reloadable.parent;
				if (thing.Map.designationManager.DesignationOn(thing, YCC_DesignationDefOf.YCC_EjectAmmo) == null)
				{
					yield return new Command_Action
					{
						defaultLabel = "YCC.EjectAmmo_label".Translate(),
						defaultDesc = "YCC.EjectAmmo_desc".Translate(),
						icon = Textures.AmmoEject,
						disabled = reloadable.EjectableAmmo() <= 0,
						disabledReason = "YCC.NoEjectableAmmo".Translate(),
						action = () => thing.Map.designationManager.AddDesignation(new Designation(thing, YCC_DesignationDefOf.YCC_EjectAmmo)),
						activateSound = YCC_SoundDefOf.YCC_Designate_EjectAmmo,
					};
				}
			}

			foreach (var gizmo in __result)
				yield return gizmo;
		}


		private static void Pawn_EquipmentTracker_DropAllEquipment_Prefix(Pawn_EquipmentTracker __instance)
		{
			// only non-player pawns
			if (__instance?.pawn?.IsPlayerControlled != false)
				return;
			// skip if all ammo is dropped
			if (YayosCombatContinued.Settings.AmmoInWeaponOnDownedFactor == 100)
				return;

			// reduce ammo in weapon
			foreach (var thing in __instance.equipment)
				TryReduceWeaponAmmo(thing);
		}

		private static void Pawn_InventoryTracker_DropAllNearPawn_Prefix(Pawn_InventoryTracker __instance)
		{
			// only non-player pawns
			if (__instance?.pawn?.IsPlayerControlled != false)
				return;
			// skip if all ammo is dropped
			if (YayosCombatContinued.Settings.AmmoDroppedOnDownedFactor == 100
				&& YayosCombatContinued.Settings.AmmoInWeaponOnDownedFactor == 100)
				return;

			// iterate through inventory
			for (int i = __instance.innerContainer.Count - 1; i >= 0; i--)
			{
				var thing = __instance.innerContainer[i];
				switch (TryReduceAmmoStackCount(thing))
				{
					case -1:	// thing is not ammo, check if it is a reloadable weapon
						TryReduceWeaponAmmo(thing);
						break;
					case 0:		// remove empty ammo stack from inventory
						__instance.innerContainer.Remove(thing);
						break;
				}
			}
		}
		private static int TryReduceAmmoStackCount(Thing thing)
		{
			// only reduce dropped ammo
			if (!thing.IsAmmo(true))
				return -1;
			// reduce dropped ammo
			var count = Mathf.RoundToInt(thing.stackCount * YayosCombatContinued.Settings.AmmoDroppedOnDownedFactor * 0.01f);
			if (count > 0)
				thing.stackCount = count;
			return count;
		}
		private static bool TryReduceWeaponAmmo(Thing thing)
		{
			var comp = thing?.TryGetComp<CompApparelReloadable>();
			// only reduce ammo if applicable
			if (comp?.AmmoDef?.IsAmmo() != true)
				return false;
			// reduce ammo in dropped weapon
			comp.remainingCharges = Mathf.RoundToInt(comp.remainingCharges * YayosCombatContinued.Settings.AmmoInWeaponOnDownedFactor * 0.01f);
			return true;
		}


		private static float JobGiver_Reload_GetPriority_Postfix(float __result, Pawn pawn)
		{
			// do not reload if hunting failed "Incompletable", it probably timed out and we don't want pawns running back and forth between reloading & hunting
			if (PawnPreviousJobTable.TryGetValue(pawn, out var jobInfo)
				&& jobInfo.Def == JobDefOf.Hunt
				&& jobInfo.EndCondition == JobCondition.Incompletable)
				__result = -1f;
			return __result;
		}
		private static void Pawn_JobTracker_EndCurrentJob_Prefix(Pawn ___pawn, Job ___curJob, JobCondition condition)
		{
			if (___pawn?.IsColonist != true
				|| ___curJob == null)
				return;

			var jobInfo = PawnPreviousJobTable.GetOrCreateValue(___pawn);
			jobInfo.Def = ___curJob.def;
			jobInfo.EndCondition = condition;

			if (YayosCombatContinued.SimpleSidearmsCompatibility
				&& ___curJob.def == JobDefOf.Reload
				&& jobInfo.PreviousWeapon != null)
			{
				// reequip previous weapon
				___pawn.EquipThingFromInventory(jobInfo.PreviousWeapon);
			}
		}

		private static void JobDriver_Reload_MakeNewToils_Prefix(JobDriver_Reload __instance)
		{
			var pawn = __instance.pawn;
			var comp = __instance.Gear?.TryGetComp<CompApparelReloadable>();
			var thing = comp?.parent;
			if (pawn == null
				|| comp == null
				|| comp.Wearer == pawn
				|| thing == null
				|| !pawn.inventory.Contains(thing))
				return;

			// remember previous weapon to reequip it after the job ended
			var jobInfo = PawnPreviousJobTable.GetOrCreateValue(pawn);
			jobInfo.PreviousWeapon = pawn.equipment.Primary;

			// thing to reload must be equipped
			pawn.EquipThingFromInventory(thing);
		}
	}
}
