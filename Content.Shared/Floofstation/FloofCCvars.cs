using Robust.Shared.Configuration;

namespace Content.Shared.FloofStation;

/// <summary>
/// Floofstation specific cvars.
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming - Shush you
public sealed class FloofCCVars
{
    public static readonly CVarDef<bool> VoreSoundEnabled =
            CVarDef.Create("ambience.vore_sound_enabled", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
