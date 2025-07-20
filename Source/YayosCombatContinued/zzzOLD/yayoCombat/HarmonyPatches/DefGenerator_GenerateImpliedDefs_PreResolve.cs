using HarmonyLib;
using RimWorld;

namespace yayoCombat.HarmonyPatches;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public class DefGenerator_GenerateImpliedDefs_PreResolve
{
    public static void Prefix()
    {
        yayoCombat.patchDef1();
    }
}