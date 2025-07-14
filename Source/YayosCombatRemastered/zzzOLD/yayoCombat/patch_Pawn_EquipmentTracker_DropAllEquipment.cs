using HarmonyLib;
using Verse;

namespace yayoCombat;

[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.DropAllEquipment))]
internal class patch_Pawn_EquipmentTracker_DropAllEquipment
{
    [HarmonyPrefix]
    private static void Prefix(Pawn_EquipmentTracker __instance, ThingOwner<ThingWithComps> ___equipment)
    {
        if (!yayoCombat.ammo || __instance.pawn.Faction?.IsPlayer == true)
        {
            return;
        }

        foreach (var thing in ___equipment)
        {
            reloadUtility.TryThingEjectAmmoDirect(thing, true, __instance.pawn);
        }
    }
}