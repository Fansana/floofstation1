namespace Content.Server.Books
{
    [RegisterComponent]
    public sealed partial class HyperlinkBookComponent : Component // Floof - M3739 - #607
    {
        [DataField("url")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string URL = string.Empty;
    }
}
