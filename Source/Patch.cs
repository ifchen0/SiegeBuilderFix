using HarmonyLib;
using RimWorld;
using Verse;

namespace SiegeBuilderFix
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            new Harmony("ifchen0.siegebuilderfix").PatchAll();
        }
    }

    /// <summary>
    /// A siege lord picks builders among its pawns with CanBeBuilder, which only checks that Construction and
    /// Firefighting are not disabled. Work types are only ever disabled for humanlikes and for colony mechs, so a
    /// non-player mech (or any pawn without a skill tracker) passes, and SetAsBuilder then throws on pawn.skills.
    /// Vanilla never puts such pawns in a siege, but mods that add mechs to outlander or pirate pawn groups do.
    /// </summary>
    public static class BuilderRules
    {
        public static bool CanBuild(Pawn p) => p.skills != null && p.workSettings != null;
    }

    [HarmonyPatch(typeof(LordToil_Siege), "CanBeBuilder")]
    public static class Patch_LordToil_Siege_CanBeBuilder
    {
        public static void Postfix(Pawn p, ref bool __result)
        {
            if (__result && !BuilderRules.CanBuild(p))
                __result = false;
        }
    }

    /// <summary>
    /// Pawns that already got the Build duty (for example in a save made after the error started) are re-selected as
    /// builders without CanBeBuilder; make them defenders instead. Also covers CanBeBuilder being inlined by the JIT.
    /// </summary>
    [HarmonyPatch(typeof(LordToil_Siege), "SetAsBuilder")]
    public static class Patch_LordToil_Siege_SetAsBuilder
    {
        public static bool Prefix(LordToil_Siege __instance, Pawn p)
        {
            if (BuilderRules.CanBuild(p))
                return true;
            Traverse.Create(__instance).Method("SetAsDefender", p).GetValue();
            __instance.rememberedDuties[p] = DutyDefOf.Defend;
            return false;
        }
    }
}
