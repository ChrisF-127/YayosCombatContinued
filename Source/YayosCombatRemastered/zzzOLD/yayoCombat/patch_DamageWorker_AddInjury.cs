using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace yayoCombat;

[HarmonyPatch(typeof(DamageWorker_AddInjury), nameof(DamageWorker_AddInjury.ApplyDamageToPart))]
[HarmonyPatch([
    typeof(DamageInfo),
    typeof(Pawn),
    typeof(DamageWorker.DamageResult)
])]
internal class patch_DamageWorker_AddInjury
{
    [HarmonyPrefix]
    private static bool Prefix(DamageWorker_AddInjury __instance, DamageInfo dinfo, Pawn pawn,
        DamageWorker.DamageResult result)
    {
        if (!yayoCombat.advArmor)
        {
            return true;
        }

        var exactPartFromDamageInfo = GetExactPartFromDamageInfo(dinfo, pawn);
        if (exactPartFromDamageInfo == null)
        {
            return false;
        }

        dinfo.SetHitPart(exactPartFromDamageInfo);
        var num = dinfo.Amount;
        var deflectedByMetalArmor = false;
        if (dinfo is { InstantPermanentInjury: false, IgnoreArmor: false })
        {
            var damageDef = dinfo.Def;
            var diminishedByMetalArmor = false;
            num = ArmorUtility.GetPostArmorDamage(pawn, num, dinfo.ArmorPenetrationInt, dinfo.HitPart, ref damageDef,
                ref deflectedByMetalArmor, ref diminishedByMetalArmor, dinfo);
            dinfo.Def = damageDef;
            if (num < dinfo.Amount)
            {
                result.diminished = true;
                result.diminishedByMetalArmor = diminishedByMetalArmor;
            }
        }

        if (dinfo.Def.ExternalViolenceFor(pawn))
        {
            num *= pawn.GetStatValue(StatDefOf.IncomingDamageFactor);
        }

        if (deflectedByMetalArmor)
        {
            result.deflected = true;
            result.deflectedByMetalArmor = true;
        }

        if (num <= 0f)
        {
            result.AddPart(pawn, dinfo.HitPart);
            return false;
        }

        if (IsHeadshot(dinfo))
        {
            result.headshot = true;
        }

        if (dinfo.InstantPermanentInjury &&
            (HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart)
                 .CompPropsFor(typeof(HediffComp_GetsPermanent)) == null ||
             dinfo.HitPart.def.permanentInjuryChanceFactor == 0f ||
             pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(dinfo.HitPart)))
        {
            return false;
        }

        if (!dinfo.AllowDamagePropagation)
        {
            FinalizeAndAddInjury(pawn, num, dinfo, result);
            return false;
        }

        var methodInfo = typeof(DamageWorker_AddInjury).GetMethod("ApplySpecialEffectsToPart",
            BindingFlags.NonPublic | BindingFlags.Instance);
        methodInfo?.Invoke(__instance, [pawn, num, dinfo, result]);
        //__instance.ApplySpecialEffectsToPart(pawn, num, dinfo, result);
        return false;
    }

    private static BodyPartRecord GetExactPartFromDamageInfo(DamageInfo dinfo, Pawn pawn)
    {
        if (dinfo.HitPart != null)
        {
            return pawn.health.hediffSet.GetNotMissingParts().All(x => x != dinfo.HitPart) ? null : dinfo.HitPart;
        }

        var bodyPartRecord = ChooseHitPart(dinfo, pawn);
        if (bodyPartRecord == null)
        {
            Log.Warning("ChooseHitPart returned null (any part).");
        }

        return bodyPartRecord;
    }

    private static BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
    {
        return pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth);
    }

    private static bool IsHeadshot(DamageInfo dinfo)
    {
        return !dinfo.InstantPermanentInjury && dinfo.HitPart.groups.Contains(BodyPartGroupDefOf.FullHead) &&
               dinfo.Def == DamageDefOf.Bullet;
    }

    private static void FinalizeAndAddInjury(Pawn pawn, float totalDamage, DamageInfo dinfo,
        DamageWorker.DamageResult result)
    {
        if (pawn.health.hediffSet.PartIsMissing(dinfo.HitPart))
        {
            return;
        }

        var hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart);
        var hediff_Injury = (Hediff_Injury)HediffMaker.MakeHediff(hediffDefFromDamage, pawn);
        hediff_Injury.Part = dinfo.HitPart;
        hediff_Injury.sourceDef = dinfo.Weapon;
        hediff_Injury.sourceBodyPartGroup = dinfo.WeaponBodyPartGroup;
        hediff_Injury.sourceHediffDef = dinfo.WeaponLinkedHediff;
        hediff_Injury.Severity = totalDamage;
        if (dinfo.InstantPermanentInjury)
        {
            var hediffComp_GetsPermanent = hediff_Injury.TryGetComp<HediffComp_GetsPermanent>();
            if (hediffComp_GetsPermanent != null)
            {
                hediffComp_GetsPermanent.IsPermanent = true;
            }
            else
            {
                Log.Error(string.Concat(
                    "Tried to create instant permanent injury on Hediff without a GetsPermanent comp: ",
                    hediffDefFromDamage, " on ", pawn));
            }
        }

        FinalizeAndAddInjury(pawn, hediff_Injury, dinfo, result);
    }

    private static void FinalizeAndAddInjury(Pawn pawn, Hediff_Injury injury, DamageInfo dinfo,
        DamageWorker.DamageResult result)
    {
        injury.TryGetComp<HediffComp_GetsPermanent>()?.PreFinalizeInjury();
        var partHealth = pawn.health.hediffSet.GetPartHealth(injury.Part);
        if (pawn.IsColonist && !dinfo.IgnoreInstantKillProtection && dinfo.Def.ExternalViolenceFor(pawn) &&
            !Rand.Chance(Find.Storyteller.difficulty.allowInstantKillChance))
        {
            var num = injury.def.lethalSeverity > 0f ? injury.def.lethalSeverity * 1.1f : 1f;
            var min = 1f;
            var max = Mathf.Min(injury.Severity, partHealth);
            for (var i = 0; i < 7; i++)
            {
                if (!pawn.health.WouldDieAfterAddingHediff(injury))
                {
                    break;
                }

                var num2 = Mathf.Clamp(partHealth - num, min, max);
                if (DebugViewSettings.logCauseOfDeath)
                {
                    Log.Message(
                        $"CauseOfDeath: attempt to prevent death for {pawn.Name} on {injury.Part.Label} attempt:{i + 1} severity:{injury.Severity}->{num2} part health:{partHealth}");
                }

                injury.Severity = num2;
                num *= 2f;
                min = 0f;
            }
        }

        pawn.health.AddHediff(injury, null, dinfo, result);
        var num3 = Mathf.Min(injury.Severity, partHealth);
        result.totalDamageDealt += num3;
        result.wounded = true;
        result.AddPart(pawn, injury.Part);
        result.AddHediff(injury);
    }
}