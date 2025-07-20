using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace yayoCombat.HarmonyPatches;

[HarmonyPatch(typeof(StatPart_ReloadMarketValue), nameof(StatPart_ReloadMarketValue.TransformAndExplain))]
public class StatPart_ReloadMarketValue_TransformAndExplain
{
    private static bool Prefix(StatRequest req, ref float val, StringBuilder explanation)
    {
        if (req.Thing is not { def.IsRangedWeapon: true })
        {
            return true;
        }

        var CompApparelReloadable = req.Thing.TryGetComp<CompApparelReloadable>();
        if (CompApparelReloadable == null)
        {
            return true;
        }

        if (CompApparelReloadable.AmmoDef == null || CompApparelReloadable.RemainingCharges == 0)
        {
            return false;
        }

        var remainingCharges = CompApparelReloadable.RemainingCharges;
        var num = CompApparelReloadable.AmmoDef.BaseMarketValue * remainingCharges;
        val += num;
        explanation?.AppendLine(
            "StatsReport_ReloadMarketValue".Translate(CompApparelReloadable.AmmoDef.Named("AMMO"),
                remainingCharges.Named("COUNT")) + ": " + num.ToStringMoneyOffset());

        return false;
    }
}