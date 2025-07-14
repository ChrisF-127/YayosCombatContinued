using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace yayoCombat;

[HarmonyPatch(typeof(CompApparelVerbOwner_Charged), nameof(CompApparelVerbOwner_Charged.PostPostMake))]
internal class patch_CompApparelVerbOwner_Charged_PostPostMake
{
    [HarmonyPostfix]
    [HarmonyPriority(0)]
    private static void Postfix(CompApparelVerbOwner_Charged __instance, ref int ___remainingCharges)
    {
        if (!yayoCombat.ammo || !__instance.parent.def.IsWeapon)
        {
            return;
        }

        ___remainingCharges = GenTicks.TicksGame <= 5
            ? Mathf.RoundToInt(__instance.Props.maxCharges * yayoCombat.s_enemyAmmo)
            : 0;
    }
}