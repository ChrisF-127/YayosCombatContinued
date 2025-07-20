using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[DefOf]
	internal class YCC_DesignationDefOf
	{
		public static DesignationDef YCC_EjectAmmo;

		static YCC_DesignationDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(YCC_DesignationDefOf));
		}
	}
}
