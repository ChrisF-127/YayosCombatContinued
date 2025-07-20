using RimWorld;
using SimpleSidearms.rimworld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace YayosCombatContinued
{
	internal static class ReloadUtility
	{
		internal static void EjectAmmo(Pawn pawn, ThingWithComps thingWithComps)
		{
			if (!pawn.IsColonist && pawn.equipment.Primary == null)
				return;

			pawn.jobs.TryTakeOrderedJob(new Job(YCC_JobDefOf.YCC_EjectAmmo, thingWithComps) { count = 1 }, JobTag.Misc);
		}

		internal static void EjectAmmoAction(Pawn pawn, CompApparelReloadable comp)
		{
			var charges = 0;
			while (comp.RemainingCharges > 0)
			{
				comp.UsedOnce();
				charges++;
			}

			while (charges > 0)
			{
				var thing = ThingMaker.MakeThing(comp.AmmoDef);
				thing.stackCount = Mathf.Min(thing.def.stackLimit, charges) * comp.Props.ammoCountPerCharge;
				charges -= thing.stackCount;

				GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
			}

			comp.Props.soundReload.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
		}

		internal static void TryThingEjectAmmoDirect(Thing weapon, bool forbidden = false, Pawn pawn = null)
		{
			if (!weapon.def.IsWeapon)
				return;

			var comp = weapon.TryGetComp<CompApparelReloadable>();
			if (comp == null)
				return;

			var charges = 0;
			while (comp.RemainingCharges > 0)
			{
				comp.UsedOnce();
				charges++;
			}

			while (charges > 0)
			{
				var thing = ThingMaker.MakeThing(comp.AmmoDef);
				thing.stackCount = Mathf.Min(thing.def.stackLimit, charges) * comp.Props.ammoCountPerCharge;
				charges -= thing.stackCount;

				if (forbidden)
					thing.SetForbidden(true);

				if (pawn != null)
					GenPlace.TryPlaceThing(thing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
				else
					GenPlace.TryPlaceThing(thing, weapon.Position, weapon.Map, ThingPlaceMode.Near);
			}
		}

		public static Thing GetEjectableWeapon(IntVec3 c, Map m)
		{
			foreach (var thing in c.GetThingList(m))
				if (thing.TryGetComp<CompApparelReloadable>().RemainingCharges > 0)
					return thing;
			return null;
		}

		public static void TryAutoReload(CompApparelReloadable comp)
		{
			if (comp == null 
				|| comp.RemainingCharges > 0 
				|| comp.AmmoDef == null)
				return;

			var pawn = comp.Wearer;
			if (pawn == null)
				return;

			var inventory = pawn.inventory?.innerContainer?.ToList();
			if (inventory == null)
				return;

			var thingsToReload = new List<Thing>();
			foreach (var thing in inventory)
			{
				if (thing != null && thing.def == comp.AmmoDef)
					thingsToReload.Add(thing);
			}

			if (thingsToReload.Count == 0 
				&& !pawn.RaceProps.Humanlike 
				&& YayosCombatContinued.Settings.RefillMechAmmo)
			{
				var thing = ThingMaker.MakeThing(comp.AmmoDef);
				thing.stackCount = comp.MaxAmmoNeeded(true);

				pawn.inventory.innerContainer.TryAdd(thing);

				thingsToReload.Add(thing);
			}

			if (thingsToReload.Count > 0)
			{
				var reloadedThings = new List<Thing>();
				var maxAmmoNeeded = comp.MaxAmmoNeeded(true);
				for (var i = thingsToReload.Count - 1; i >= 0; i--)
				{
					var ammoToUse = Mathf.Min(maxAmmoNeeded, thingsToReload[i].stackCount);
					if (!pawn.inventory.innerContainer.TryDrop(
							thingsToReload[i], 
							pawn.Position, 
							pawn.Map, 
							ThingPlaceMode.Direct,
							ammoToUse, 
							out var resultingThing) 
						&& !pawn.inventory.innerContainer.TryDrop(
							thingsToReload[i],
							pawn.Position, 
							pawn.Map, 
							ThingPlaceMode.Near, 
							ammoToUse, 
							out resultingThing))
						continue; // cant generate item?

					if (resultingThing != null && ammoToUse > 0)
					{
						maxAmmoNeeded -= ammoToUse;
						reloadedThings.Add(resultingThing);
					}

					if (maxAmmoNeeded <= 0)
						break;
				}

				if (reloadedThings.Count <= 0)
					return;

				var job = JobMaker.MakeJob(JobDefOf.Reload, comp.ReloadableThing); // cp.parent ?
				job.targetQueueB = reloadedThings.Select(t => new LocalTargetInfo(t)).ToList();
				job.count = reloadedThings.Sum(t => t.stackCount);
				job.count = Math.Min(job.count, comp.MaxAmmoNeeded(true));

				pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
				pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));

				return;
			}

			if (YayosCombatContinued.Settings.SupplyAmmoDistance < 0)
				return;

			List<Thing> reservableThings = null;
			try
			{
				reservableThings = RefuelWorkGiverUtility.FindEnoughReservableThings(
					pawn,
					pawn.Position,
					new IntRange(comp.MinAmmoNeeded(false), comp.MaxAmmoNeeded(false)), 
					t => t.def == comp.AmmoDef && pawn.Position.DistanceTo(t.Position) <= YayosCombatContinued.Settings.SupplyAmmoDistance);
			}
			catch (Exception ex)
			{
				Log.ErrorOnce($"{pawn} cannot find ammo for {comp}\n{ex}", ex.GetHashCode());
			}

			if (reservableThings == null || pawn.jobs.jobQueue.Count > 0)
				return;

			pawn.jobs.TryTakeOrderedJob(JobGiver_Reload.MakeReloadJob(comp, reservableThings), JobTag.Misc);
			pawn.jobs.jobQueue.EnqueueLast(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));
		}


		public static bool IsCapableOfReloading(this Pawn pawn) =>
			pawn != null
			&& pawn.Spawned
			&& !pawn.Destroyed
			&& !pawn.Discarded
			&& !pawn.Downed
			&& !pawn.Dead
			&& !pawn.InMentalState
			&& !pawn.InContainerEnclosed
			&& !pawn.InCryptosleep
			&& !pawn.Deathresting
			&& pawn.CarriedBy == null;

		public static bool TryAutoReloadSingle(
			CompApparelReloadable comp,
			bool showOutOfAmmoWarning = false,
			bool showJobWarnings = false,
			bool ignoreDistance = false, 
			bool returnToStartingPosition = true)
		{
			var success = true;
			if (comp?.RemainingCharges <= 0)
			{
				if (comp.Wearer is Pawn pawn
					&& pawn.IsCapableOfReloading()
					&& comp.parent is ThingWithComps thing)
				{
					var ammoInInventory = pawn.CountAmmoInInventory(comp);

					// check if comp needs reloading - this should always be the case at this point
					var minAmmoNeeded = comp.MinAmmoNeededChecked();

					// add ammo to inventory if pawn is not humanlike; for example a mech or a llama wielding a shotgun
					if (ammoInInventory < minAmmoNeeded && !pawn.RaceProps.Humanlike && yayoCombat.yayoCombat.refillMechAmmo)
					{
						var ammo = ThingMaker.MakeThing(comp.AmmoDef);
						ammo.stackCount = comp.MaxAmmoNeeded(false);
						if (pawn.inventory.innerContainer.TryAdd(ammo))
							ammoInInventory = ammo.stackCount;
					}

					// only reload equipped weapon from inventory
					if (ammoInInventory >= minAmmoNeeded)
						success = TryReloadFromInventory(pawn, new Thing[] { thing }, showJobWarnings);
					// reload all weapons from surrounding
					else
						success = TryReloadFromSurrounding(pawn, pawn.GetAllReloadableThings(), showJobWarnings, ignoreDistance, returnToStartingPosition);
					// show out of ammo warning if reloading failed
					if (showOutOfAmmoWarning && !success && pawn.IsColonist)
						GeneralUtility.ShowRejectMessage(pawn, "YCC.OutOfAmmo".Translate(new NamedArgument(pawn, "pawn")));
				}
				else
				{
					success = false;
				}
			}
			return success;
		}
		public static bool TryAutoReloadAll(
			Pawn pawn,
			bool showOutOfAmmoWarning = false,
			bool showJobWarnings = false,
			bool ignoreDistance = false, 
			bool returnToStartingPosition = true)
		{
			var success = true;
			var things = pawn?.GetAllReloadableThings()?.ToArray();
			if (things?.AnyAtLowAmmo(pawn, false) == true)
			{
				if (pawn.IsCapableOfReloading())
				{
					var reloadFromInventory = false;
					foreach (var thing in things)
					{
						var comp = thing.TryGetComp<CompApparelReloadable>();
						var ammoInInventory = pawn.CountAmmoInInventory(comp);

						// check if comp needs reloading - this should always be the case at this point
						var minAmmoNeeded = comp.MinAmmoNeededChecked();

						// add ammo to inventory if pawn is not humanlike; for example a mech or a llama wielding a shotgun
						if (ammoInInventory < minAmmoNeeded && !pawn.RaceProps.Humanlike && yayoCombat.yayoCombat.refillMechAmmo)
						{
							var ammo = ThingMaker.MakeThing(comp.AmmoDef);
							ammo.stackCount = comp.MaxAmmoNeeded(false);
							if (pawn.inventory.innerContainer.TryAdd(ammo))
								ammoInInventory = ammo.stackCount;
						}

						// reload from inventory is there is anything that can be reloaded from inventory
						if (ammoInInventory >= minAmmoNeeded)
							reloadFromInventory = true;
					}

					// reload from inventory
					if (reloadFromInventory)
						success = TryReloadFromInventory(pawn, things, showJobWarnings);
					// reload from surrounding
					else if (things.AnyAtLowAmmo(pawn, true))
						success = TryReloadFromSurrounding(pawn, things, showJobWarnings, ignoreDistance, returnToStartingPosition);
					// show out of ammo warning if reloading failed
					if (showOutOfAmmoWarning && !success && pawn.IsColonist)
						GeneralUtility.ShowRejectMessage(pawn, "YCC.OutOfAmmo".Translate(new NamedArgument(pawn, "pawn")));
				}
				else
				{
					success = false;
				}
			}
			return success;
		}


		public static bool TryReloadFromInventory(Pawn pawn, IEnumerable<Thing> reloadables, bool showWarnings)
		{
			bool success = false;

			// check for things requiring reloading
			var ammoDefDict = reloadables.GetRequiredAmmo();
			if (ammoDefDict.Count() > 0)
			{
				// find ammo for reloading in inventory
				var ammoThings = pawn.FindAmmoThingsInventory(ammoDefDict, showWarnings);
				if (ammoThings.Count > 0)
				{
					// make job
					var job = JobMaker.MakeJob(YCC_JobDefOf.YCC_ReloadFromInventory);

					// fill job queue
					foreach (var thing in reloadables)
						job.AddQueuedTarget(TargetIndex.A, thing);
					foreach (var thing in ammoThings)
						job.AddQueuedTarget(TargetIndex.B, thing);

					// start reload job and try to resume previous job after reloading
					pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);

					success = true;
				}
			}
			else if (showWarnings && pawn.IsColonist) // nothing to reload
				GeneralUtility.ShowRejectMessage(pawn, "YCC.NothingToReload".Translate());

			return success;
		}

		public static bool TryReloadFromSurrounding(Pawn pawn, IEnumerable<Thing> reloadables, bool showWarnings, bool ignoreDistance, bool returnToStartingPosition = true)
		{
			bool success = false;
			if (!ignoreDistance && yayoCombat.yayoCombat.supplyAmmoDist < 0)
				return success;

			// check for things requiring reloading
			var ammoDefDict = reloadables.GetRequiredAmmo();
			if (ammoDefDict.Count() > 0)
			{
				// find ammo for reloading
				var ammoThings = pawn.FindAmmoThingsSurrounding(ammoDefDict, showWarnings, ignoreDistance);
				if (ammoThings.Count > 0)
				{
					// make job
					var job = JobMaker.MakeJob(YCC_JobDefOf.YCC_ReloadFromSurrounding);

					// fill job queues
					foreach (var thing in reloadables)
						job.AddQueuedTarget(TargetIndex.A, thing);
					foreach (var thing in ammoThings)
						job.AddQueuedTarget(TargetIndex.B, thing);

					// start reload job and try to resume previous job after reloading
					pawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: true, canReturnCurJobToPool: true);

					// make pawn go back to where they were
					if (returnToStartingPosition)
						pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.Goto, pawn.Position));

					success = true;
				}
			}
			else if (showWarnings && pawn.IsColonist) // nothing to reload
				GeneralUtility.ShowRejectMessage(pawn, "YCC.NothingToReload".Translate());

			return success;
		}


		public static List<Thing> GetAllReloadableThings(this Pawn pawn)
		{
			var things = new List<Thing>();
			if (pawn != null)
			{
				foreach (var thing in pawn.equipment.AllEquipmentListForReading)
					if (thing.MaxAmmoNeeded(out _) > 0)
						things.Add(thing);

				if (YayosCombatContinued.SimpleSidearmsCompatibility)
				{
					foreach (var thing in pawn.GetSimpleSidearms())
						if (thing.MaxAmmoNeeded(out _) > 0)
							things.Add(thing);
				}
			}
			return things;
		}
		public static Dictionary<Def, AmmoInfo> GetRequiredAmmo(this IEnumerable<Thing> things)
		{
			var output = new Dictionary<Def, AmmoInfo>();
			if (things != null)
			{
				foreach (var thing in things)
				{
					var count = thing.MaxAmmoNeeded(out Def def);
					if (count > 0)
					{
						var minAmmoNeeded = thing.MinAmmoNeededForThing();
						if (output.ContainsKey(def))
						{
							output[def].Count += count;
							if (output[def].MinAmmoNeeded > minAmmoNeeded)
								output[def].MinAmmoNeeded = minAmmoNeeded;
						}
						else
						{
							output.Add(def, new AmmoInfo
							{
								Count = count,
								MinAmmoNeeded = minAmmoNeeded,
							});
						}
					}
				}
			}
			return output;
		}

		public static List<Thing> FindAmmoThingsInventory(this Pawn pawn, Dictionary<Def, AmmoInfo> ammoDefDict, bool showWarnings)
		{
			var output = new List<Thing>();
			if (pawn != null && ammoDefDict != null)
			{
				foreach (var entry in ammoDefDict)
				{
					var ammoDef = entry.Key;
					var count = entry.Value.Count;
					var minAmmoNeeded = entry.Value.MinAmmoNeeded;

					// find things for this ammoDef in inventory
					foreach (var thing in pawn.inventory.innerContainer)
					{
						if (thing.def == ammoDef)
						{
							if (count <= 0)
								break;
							output.Add(thing);
							count -= thing.stackCount;
						}
					}
					// if less than minAmmoNeeded was found, remove all things of this ammoDef from output
					if ((entry.Value.Count - count) < minAmmoNeeded)
					{
						for (int i = output.Count - 1; i >= 0; i--)
							if (output[i].def == ammoDef)
								output.RemoveAt(i);
					}
					// show warning if ammo not found
					if (showWarnings && count > 0 && pawn.IsColonist)
					{
						GeneralUtility.ShowRejectMessage(
							pawn,
							"YCC.NoAmmoInventory".Translate(
								new NamedArgument(pawn, "pawn"),
								new NamedArgument(ammoDef.label, "ammo"),
								new NamedArgument(count, "count"),
								new NamedArgument(minAmmoNeeded, "minAmmoNeeded")));
					}
				}
			}
			return output;
		}
		public static List<Thing> FindAmmoThingsSurrounding(this Pawn pawn, Dictionary<Def, AmmoInfo> ammoDefDict, bool showWarnings, bool ignoreDistance)
		{
			var output = new List<Thing>();
			if (pawn != null && ammoDefDict != null)
			{
				foreach (var entry in ammoDefDict)
				{
					var ammoDef = entry.Key;
					var count = entry.Value.Count;
					var minAmmoNeeded = entry.Value.MinAmmoNeeded;

					// find things for this ammoDef nearby
					var things = RefuelWorkGiverUtility.FindEnoughReservableThings(
						pawn,
						pawn.Position,
						new IntRange(minAmmoNeeded, count),
						t => t.def == ammoDef && (ignoreDistance || IntVec3Utility.DistanceTo(pawn.Position, t.Position) <= yayoCombat.yayoCombat.supplyAmmoDist));

					// add found things to output
					if (things?.Count > 0)
					{
						foreach (var thing in things)
							output.Add(thing);
					}
					// show warning if ammo not found
					else if (showWarnings && pawn.IsColonist)
					{
						GeneralUtility.ShowRejectMessage(
							pawn,
							"YCC.NoAmmoNearby".Translate(
								new NamedArgument(pawn, "pawn"),
								new NamedArgument(ammoDef.label, "ammo"),
								new NamedArgument(count, "count"),
								new NamedArgument(minAmmoNeeded, "minAmmoNeeded")));
					}
				}
			}
			return output;
		}

		// Helper class for picking up ammo
		public class AmmoInfo
		{
			public int Count;
			public int MinAmmoNeeded;
		}
	}
}
