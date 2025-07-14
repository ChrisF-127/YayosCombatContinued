using HarmonyLib;
using RimWorld;
using RimWorld.Utility;
using Verse;

namespace yayoCombat;

[HarmonyPatch(typeof(ReloadableUtility), nameof(ReloadableUtility.OwnerOf))]
internal class patch_ReloadableUtility_WearerOf
{
    [HarmonyPostfix]
    private static void Postfix(ref Pawn __result, IReloadableComp reloadable)
    {
        if (!yayoCombat.ammo || __result != null)
        {
            return;
        }

        if (reloadable is CompApparelReloadable
            {
                ParentHolder: Pawn_EquipmentTracker { pawn: not null } equipmentTracker
            })
        {
            __result = equipmentTracker.pawn;
        }
    }
}