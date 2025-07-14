using Verse;

namespace yayoCombat;

internal class Stance_yyReload : Stance_Cooldown
{
    public Stance_yyReload()
    {
    }

    public Stance_yyReload(int ticks, LocalTargetInfo focusTarg, Verb verb)
        : base(ticks, focusTarg, verb)
    {
    }

    public override bool StanceBusy => false;
}