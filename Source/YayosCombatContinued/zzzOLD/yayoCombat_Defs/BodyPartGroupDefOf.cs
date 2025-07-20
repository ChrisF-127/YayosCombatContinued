using RimWorld;
using Verse;

namespace yayoCombat_Defs;

[DefOf]
public static class BodyPartGroupDefOf
{
    public static BodyPartGroupDef Torso;

    public static BodyPartGroupDef UpperHead;

    public static BodyPartGroupDef FullHead;

    public static BodyPartGroupDef Shoulders;

    public static BodyPartGroupDef Arms;

    public static BodyPartGroupDef Hands;

    public static BodyPartGroupDef LeftHand;

    public static BodyPartGroupDef RightHand;

    public static BodyPartGroupDef Legs;

    public static BodyPartGroupDef Feet;

    static BodyPartGroupDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BodyPartGroupDefOf));
    }
}