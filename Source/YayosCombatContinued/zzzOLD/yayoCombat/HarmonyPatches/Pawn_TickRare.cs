using HarmonyLib;
using RimWorld;
using Verse;

namespace yayoCombat.HarmonyPatches;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Tick))]
internal class Pawn_TickRare
{
    [HarmonyPriority(0)]
    private static void Postfix(Pawn __instance)
    {
        if (!yayoCombat.ammo || !__instance.Drafted || Find.TickManager.TicksGame % 60 != 0 ||
            __instance.CurJobDef != JobDefOf.Wait_Combat && __instance.CurJobDef != JobDefOf.AttackStatic &&
            __instance.CurJobDef != JobDefOf.Reload ||
            __instance.equipment == null)
        {
            return;
        }

        var allEquipmentListForReading = __instance.equipment.AllEquipmentListForReading;
        foreach (var item in allEquipmentListForReading)
        {
            var CompApparelReloadable = item.TryGetComp<CompApparelReloadable>();
            if (CompApparelReloadable == null)
            {
                continue;
            }

            reloadUtility.tryAutoReload(CompApparelReloadable);
            break;
        }
    }
}