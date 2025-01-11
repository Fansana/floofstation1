using System.Text;
using Content.Server.FloofStation.Speech.Components;
using Content.Server.Speech;
using Robust.Shared.Random;


namespace Content.Server.FloofStation.Speech.EntitySystems;


public sealed class DrunkardAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrunkardAccentComponent, AccentGetEvent>(OnAccent);
    }

    // A modified copy of SlurredSystem's Accentuate.
    public string Accentuate(string message)
    {
        var sb = new StringBuilder();

        foreach (var character in message)
        {
            if (_random.Prob(0.1f))
            {
                var lower = char.ToLowerInvariant(character);
                var newString = lower switch
                {
                    'o' => "u",
                    's' => "ch",
                    'a' => "ah",
                    'u' => "oo",
                    'c' => "k",
                    _ => $"{character}",
                };

                sb.Append(newString);
            }

            if (!_random.Prob(0.05f))
            {
                sb.Append(character);
                continue;
            }

            var next = _random.Next(1, 3) switch
            {
                1 => "'",
                2 => $"{character}{character}",
                _ => $"{character}{character}{character}",
            };

            sb.Append(next);
        }

        return sb.ToString();
    }

    private void OnAccent(EntityUid uid, DrunkardAccentComponent component, AccentGetEvent args) =>
        args.Message = Accentuate(args.Message);
}
