using HarmonyLib;
using RimWorld;

namespace yayoCombat;

[HarmonyPatch(typeof(Building_TurretGun), nameof(Building_TurretGun.MakeGun))]
internal class patch_Building_TurretGun_MakeGun
{
    [HarmonyPrefix]
    private static bool Prefix(Building_TurretGun __instance)
    {
        if (!yayoCombat.ammo)
        {
            return true;
        }

        var turretGunDef = __instance.def.building.turretGunDef;
        var removedReload = false;
        for (var i = 0; i < turretGunDef.comps.Count; i++)
        {
            if (turretGunDef.comps[i].compClass != typeof(CompApparelReloadable))
            {
                continue;
            }

            turretGunDef.comps.Remove(turretGunDef.comps[i]);
            removedReload = true;
        }

        if (removedReload)
        {
            __instance.def.building.turretGunDef = turretGunDef;
        }

        return true;
    }
}