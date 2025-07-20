using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[DefOf]
	internal static class YCC_ApparelLayerDefOf
	{
		public static ApparelLayerDef OnSkin_A;
		public static ApparelLayerDef Shell_A;
		public static ApparelLayerDef Middle_A;
		public static ApparelLayerDef Belt_A;
		public static ApparelLayerDef Overhead_A;

		static YCC_ApparelLayerDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(YCC_ApparelLayerDefOf));
		}
	}
}
