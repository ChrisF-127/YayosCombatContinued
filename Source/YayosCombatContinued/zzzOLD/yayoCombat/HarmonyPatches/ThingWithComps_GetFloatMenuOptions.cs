using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace yayoCombat.HarmonyPatches;

[HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetFloatMenuOptions))]
internal class ThingWithComps_GetFloatMenuOptions
{
    [HarmonyPriority(0)]
    private static void Postfix(ref IEnumerable<FloatMenuOption> __result, ThingWithComps __instance, Pawn selPawn)
    {
        if (!yayoCombat.ammo)
        {
            return;
        }

        var CompApparelReloadable = __instance.TryGetComp<CompApparelReloadable>();
        if (selPawn.IsColonist && CompApparelReloadable is { AmmoDef: not null } &&
            !CompApparelReloadable.Props.destroyOnEmpty &&
            CompApparelReloadable.RemainingCharges > 0)
        {
            __result = new List<FloatMenuOption>
            {
                new FloatMenuOption(
                    "eject_AmmoAmount".Translate(CompApparelReloadable.RemainingCharges,
                        CompApparelReloadable.AmmoDef.LabelCap), cleanWeapon, MenuOptionPriority.High)
            };
        }

        return;

        void cleanWeapon()
        {
            reloadUtility.EjectAmmo(selPawn, __instance);
        }
    }
}