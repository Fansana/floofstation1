namespace Content.Server.Floof
{
    [RegisterComponent]
    public sealed partial class CharConsentComponent : Component
    {
        [DataField("consent", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Consent = "";
    }
}
