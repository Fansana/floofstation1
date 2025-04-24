using Content.Server.Paint;


namespace Content.Server._Floof.Paint;


public sealed class PaintOnInitSystem : EntitySystem
{
    [Dependency] private readonly PaintSystem _paint = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PaintOnInitComponent, MapInitEvent>(OnInit);
    }

    private void OnInit(Entity<PaintOnInitComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Color is not { } color)
            return;

        _paint.Paint(whitelist: null, blacklist: null, target: ent, color: color);
    }
}
