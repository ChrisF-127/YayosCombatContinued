using HarmonyLib;
using RimWorld;

namespace yayoCombat;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public class patch_DefGenerator_GenerateImpliedDefs_PreResolve
{
    public static void Prefix()
    {
        yayoCombat.patchDef1();
    }
}