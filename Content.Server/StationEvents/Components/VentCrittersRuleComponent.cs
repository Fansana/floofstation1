using System.Numerics;
using Content.Server.StationEvents.Events;
using Content.Shared.Storage;
using Robust.Shared.Audio;


namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(VentCrittersRule))]
public sealed partial class VentCrittersRuleComponent : Component
{
    [DataField("entries")]
    public List<EntitySpawnEntry> Entries = new();

    /// <summary>
    /// At least one special entry is guaranteed to spawn
    /// </summary>
    [DataField("specialEntries")]
    public List<EntitySpawnEntry> SpecialEntries = new();

    // Floof section - delayed spawns

    /// <summary>
    ///     The random per-critter delay between the start of the event and their spawn, in seconds.
    /// </summary>
    [DataField]
    public Vector2 InitialDelay = new(0, 20);

    /// <summary>
    ///     An optional delay after showing the popup and playing the sound, but before spawning, in seconds.
    /// </summary>
    [DataField]
    public Vector2? CrawlTime = null;

    /// <summary>
    ///     An optional popup to show after the initial delay.
    /// </summary>
    [DataField]
    public LocId? Popup = null;

    /// <summary>
    ///     An optional sound to play after the initial delay.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound = null;

    // Floof section end
}
