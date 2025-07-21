using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[HarmonyPatch(typeof(StatPart_ReloadMarketValue), nameof(StatPart_ReloadMarketValue.TransformAndExplain))]
	public class StatPart_ReloadMarketValue_TransformAndExplain
	{
		private static bool Prefix(StatRequest req, ref float val, StringBuilder explanation)
		{
			if (!req.Thing.def.IsRangedWeapon)
				return true; // don't skip

			var comp = req.Thing.TryGetComp<CompApparelReloadable>();
			if (comp == null)
				return true; // don't skip

			if (comp.AmmoDef == null)
				return false; // skip

			var remainingCharges = comp.RemainingCharges;
			if (remainingCharges == 0) 
				return false; // skip

			var marketValue = comp.AmmoDef.BaseMarketValue * remainingCharges;
			val += marketValue;
			explanation?.AppendLine("StatsReport_ReloadMarketValue".Translate(comp.AmmoDef.Named("AMMO"), remainingCharges.Named("COUNT")) + ": " + marketValue.ToStringMoneyOffset());

			return false; // skip
		}
	}
}
