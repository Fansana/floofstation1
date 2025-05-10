using Robust.Shared.GameStates;


namespace Content.Shared._Floof.Clothing.ClothingLimit;


/// <summary>
///     Restricts the number of items of a particular group an entity can equip at once.
///
///     Intended to be used on items that can be equipped in multiple slots, but that
///     should only be worn in one of them at a time.
/// </summary>
/// <example>See /Resources/Prototypes/Floof/Entities/Clothing/Uncategorized/harness.yml</example>
[RegisterComponent]
public sealed partial class ClothingLimitComponent : Component
{
    /// <summary>
    ///     Names of groups this clothing belongs to. Case-sensetive and all that.
    /// </summary>
    [DataField(required: true)]
    public HashSet<string> LimitGroups = null!;

    /// <summary>
    ///     Maximum number of items of this group that can be equipped at once.
    /// </summary>
    [DataField(required: true)]
    public int MaxCount;

    /// <summary>
    ///     Whether to also count non-equipped clothing (e.g. a neck-slot item lying in one's pocket)
    /// </summary>
    [DataField]
    public bool CheckNonEquipped = false;
}
