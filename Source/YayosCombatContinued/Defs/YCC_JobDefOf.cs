using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[DefOf]
	internal class YCC_JobDefOf
	{
		public static JobDef YCC_EjectAmmo;
		public static JobDef YCC_ReloadFromInventory;
		public static JobDef YCC_ReloadFromSurrounding;

		static YCC_JobDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(YCC_JobDefOf));
		}
	}
}
