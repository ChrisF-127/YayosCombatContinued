using RimWorld;
using Verse;

namespace YayosCombatContinued
{
	[DefOf]
	internal class YCC_SoundDefOf
	{
		public static SoundDef YCC_Designate_EjectAmmo;

		static YCC_SoundDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(YCC_SoundDefOf));
		}
	}
}
