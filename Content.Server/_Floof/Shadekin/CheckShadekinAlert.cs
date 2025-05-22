using Content.Shared._Floof.Shadekin;
using Content.Server.Chat.Managers;
using Content.Shared.Alert;
using Content.Shared.Chat;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Player;
using Content.Shared.FixedPoint;

namespace Content.Server._Floof.Shadekin;

[UsedImplicitly]
[DataDefinition]
public sealed partial class CheckShadekinAlert : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var chatManager = IoCManager.Resolve<IChatManager>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var playerManager = IoCManager.Resolve<IPlayerManager>();

        if (!entityManager.TryGetComponent(player, out ShadekinComponent? shadekin) ||
            !playerManager.TryGetSessionByEntity(player, out var session))
            return;

        if (shadekin.Blackeye)
            SendMessage(chatManager, Loc.GetString("shadekinenergy-alert-blackeye"), session);
        else
            SendMessage(chatManager, Loc.GetString("shadekinenergy-alert-energy", ("energy", FixedPoint2.Min(shadekin.Energy, shadekin.MaxEnergy)), ("energyMax", shadekin.MaxEnergy)), session);

        SendMessage(chatManager, Loc.GetString("shadekinenergy-alert-" + shadekin.LightExposure), session);
    }

    private static void SendMessage(IChatManager chatManager, string msg, ICommonSession session)
    {
        chatManager.ChatMessageToOne(ChatChannel.Emotes,
            msg,
            msg,
            EntityUid.Invalid,
            false,
            session.Channel);
    }
}
