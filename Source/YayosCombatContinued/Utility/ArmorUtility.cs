using RimWorld;
using UnityEngine;
using Verse;

namespace YayosCombatContinued
{
	internal static class ArmorUtility
	{
		public const float MaxArmorRating = 2f;
		public const float DeflectThresholdFactor = 0.5f;

		public static float GetPostArmorDamage(
			Pawn pawn, 
			float amount, 
			float armorPenetration, 
			BodyPartRecord part,
			ref DamageDef damageDef, 
			ref bool deflectedByMetalArmor, 
			ref bool diminishedByMetalArmor, 
			DamageInfo damageInfo)
		{
			bool forcedDeflection;
			bool metalArmor;
			float oriAmount;

			deflectedByMetalArmor = false;
			diminishedByMetalArmor = false;

			if (damageDef.armorCategory == null)
				return amount;

			var armorRatingStat = damageDef.armorCategory.armorRatingStat;
			if (pawn.apparel != null)
			{
				var wornApparel = pawn.apparel.WornApparel;
				for (var num = wornApparel.Count - 1; num >= 0; num--)
				{
					var apparel = wornApparel[num];
					if (!apparel.def.apparel.CoversBodyPart(part))
						continue;

					oriAmount = amount;
					ApplyArmor(
						ref amount, 
						armorPenetration, 
						apparel.GetStatValue(armorRatingStat), 
						apparel,
						ref damageDef, 
						pawn, 
						out metalArmor, 
						damageInfo, 
						out forcedDeflection);

					if (amount < 0.001f)
					{
						deflectedByMetalArmor = metalArmor || forcedDeflection;
						return 0f;
					}

					if (amount < oriAmount && metalArmor)
						diminishedByMetalArmor = true;
				}
			}

			oriAmount = amount;
			ApplyArmor(
				ref amount, 
				armorPenetration,
				pawn.GetStatValue(armorRatingStat),
				null, 
				ref damageDef, 
				pawn,
				out metalArmor, 
				damageInfo, 
				out forcedDeflection);

			if (amount < 0.001f)
			{
				deflectedByMetalArmor = metalArmor || forcedDeflection;
				return 0f;
			}

			if (amount < oriAmount && metalArmor)
				diminishedByMetalArmor = true;

			if (forcedDeflection)
				deflectedByMetalArmor = true;

			return amount;
		}

		public static void ApplyArmor(
			ref float damageAmount, 
			float armorPenetration, 
			float armorRating, 
			Thing armorThing,
			ref DamageDef damageDef, 
			Pawn pawn, 
			out bool metalArmor, 
			DamageInfo damageInfo, 
			out bool forcedDeflection)
		{
			var isArmor = false;
			var isMechanoid = pawn.RaceProps.IsMechanoid;

			forcedDeflection = false;

			if (armorThing == null)
				metalArmor = isMechanoid;
			else
			{
				isArmor = true;
				metalArmor = armorThing.def.apparel.useDeflectMetalEffect || armorThing.Stuff.IsMetal == true;
			}

			if (isArmor && damageInfo.Weapon != null)
			{
				if (armorThing.def.techLevel >= TechLevel.Spacer || isMechanoid)
				{
					if (damageInfo.Weapon.IsMeleeWeapon)
					{
						if (damageInfo.Weapon.techLevel <= TechLevel.Medieval)
							armorPenetration *= 0.5f;
					}
					else if (damageInfo.Weapon.techLevel <= TechLevel.Medieval)
						armorPenetration *= 0.35f;
				}
				else if (armorThing.def.techLevel >= TechLevel.Industrial)
				{
					if (damageInfo.Weapon.IsMeleeWeapon)
					{
						if (damageInfo.Weapon.techLevel <= TechLevel.Neolithic)
							armorPenetration *= 0.75f;
					}
					else if (damageInfo.Weapon.techLevel <= TechLevel.Medieval)
						armorPenetration *= 0.5f;
				}
			}

			var leftArmor = Mathf.Max(armorRating - armorPenetration, 0f);
			var armorDamage = Mathf.Clamp01((armorPenetration - (armorRating * 0.15f)) * 5f);
			var randomZeroOne = Rand.Value;

			if (isArmor)
			{
				var f = damageAmount * (0.2f + (armorDamage * DeflectThresholdFactor));
				armorThing.TakeDamage(new DamageInfo(damageDef, GenMath.RoundRandom(f)));
			}

			var armorHpPer = !isArmor
				? pawn.health.summaryHealth.SummaryHealthPercent
				: armorThing.HitPoints / (float)armorThing.MaxHitPoints;
			var defenceRating = Mathf.Max((armorRating * 0.9f) - armorPenetration, 0f);
			var getHitRating = 1f - YayosCombatContinued.Settings.ArmorEfficiency;

			if (randomZeroOne * getHitRating < defenceRating * armorHpPer)
			{
				if (Rand.Value < Mathf.Min(leftArmor, 0.9f))
					damageAmount = 0f;
				else if (isArmor)
				{
					damageAmount = GenMath.RoundRandom(damageAmount * (0.25f + armorDamage * 0.25f));

					if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
						damageDef = DamageDefOf.Blunt;

					forcedDeflection = true;
				}
				else
				{
					damageAmount = GenMath.RoundRandom(damageAmount * (0.25f + armorDamage * DeflectThresholdFactor));

					if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
						damageDef = DamageDefOf.Blunt;

					forcedDeflection = true;
				}
			}
			else if (randomZeroOne < leftArmor * (0.5f + armorHpPer * DeflectThresholdFactor))
			{
				damageAmount = GenMath.RoundRandom(damageAmount * DeflectThresholdFactor);

				if (damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp)
					damageDef = DamageDefOf.Blunt;
			}
		}
	}
}
