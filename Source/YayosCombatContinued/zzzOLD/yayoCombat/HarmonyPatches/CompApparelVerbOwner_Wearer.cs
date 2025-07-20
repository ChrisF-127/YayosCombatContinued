using HarmonyLib;
using RimWorld;
using Verse;

namespace yayoCombat.HarmonyPatches;

[HarmonyPatch(typeof(CompApparelVerbOwner), nameof(CompApparelVerbOwner.Wearer), MethodType.Getter)]
public class CompApparelVerbOwner_Wearer
{
    // Added to allow CompApparelVerbOwner (reloadable) to get the owner when fetching gizmos this is only intended to work with Apparel by default i guess?
    private static void Postfix(ref Pawn __result, CompApparelVerbOwner __instance)
    {
        if (__result == null && __instance.ParentHolder is Pawn_EquipmentTracker tracker)
        {
            __result = tracker.pawn;
        }
    }
}