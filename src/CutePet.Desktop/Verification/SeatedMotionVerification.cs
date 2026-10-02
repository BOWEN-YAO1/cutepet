using System;
using System.Linq;

namespace CutePet.Desktop;

internal static class SeatedMotionVerification
{
    internal static void Run(MainWindow window, Action<bool, string> check)
    {
        var pack = window.Characters.Find("tianyi");
        var player = new CharacterAnimation();
        foreach (var action in new[] { "sit-blink", "sit-greeting", "sit-happy" })
        {
            player.Configure(pack);
            player.Preview(action);
            check(player.Action == action && player.RestPose && player.Resting, action + " preview preserves the seated base");
            player.Advance(TimeSpan.FromMilliseconds(pack.Actions[action].Duration));
            check(player.Action == "sit" && player.Image == pack.Actions["sit"].Frames[0].Image,
                action + " finishes at the original seated image");
            player.Preview(action);
            player.Low = true;
            check(player.Action == "low" && !player.RestPose, "fresh low quota cancels " + action);
            player.Configure(pack);
            player.Preview(action);
            player.Reset();
            player.Advance(TimeSpan.FromSeconds(2));
            check(player.Action == "idle" && !player.RestPose, "lifetime reset cancels " + action);
        }
        player.Configure(pack);
        check(!player.TryAmbient("sit-happy"), "seated ambient expression cannot start while standing");
        player.Preview("sit");
        check(player.TryAmbient("sit-happy") && !player.TryAmbient("sit-happy"), "seated ambient expression only starts when settled");
        player.Blink();
        check(player.Action == "sit-happy", "blink never interrupts a seated response");
        player.ReactToClick(0);
        player.Advance(TimeSpan.FromMilliseconds(200));
        player.ReactToClick(1);
        check(player.Action == "sit-happy" && player.Resting, "latest seated click replaces the reply without a standing queue");
        player.Advance(TimeSpan.FromSeconds(10));
        check(player.Action == "sit" && player.Resting, "large elapsed time finishes the seated reply without standing");
        player.Greet();
        check(player.Action == "sit-greeting" && player.Resting, "explicit greeting also uses seated art");
        player.StandUp();
        check(player.Action == "stand" && !player.Resting, "manual rise cancels a seated reply");
        player.Advance(TimeSpan.FromSeconds(3));
        check(player.Action == "idle" && !player.RestPose, "manual rise never replays a cancelled seated reply");
        foreach (var action in new[] { "sit-greeting", "sit-happy" })
        {
            var single = pack with { Actions = pack.Actions.Where(pair => !pair.Key.StartsWith("sit-", StringComparison.Ordinal) || pair.Key == action)
                .ToDictionary(pair => pair.Key, pair => pair.Value) };
            player.Configure(single);
            player.Preview("sit");
            player.ReactToClick(action == "sit-greeting" ? 1 : 0);
            check(player.Action == action && player.Resting, "single seated reply is used regardless of random choice: " + action);
        }
        window.SetCharacter(PetCharacter.Tianyi);
        window.WakeCharacterImmediately();
        window.AdvanceAmbient(TimeSpan.FromSeconds(30));
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
        window.PlayCharacterInteraction();
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
        window.AdvanceAmbient(TimeSpan.FromSeconds(20));
        check(window.CurrentCharacterFrame == CharacterFrame.Rise,
            "seated clicks preserve automatic rest and its eventual scheduled rise");
        window.WakeCharacterImmediately();
        window.ToggleCharacterRest();
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
        window.BeginDetailsMenu();
        window.AdvanceAmbient(TimeSpan.FromSeconds(16));
        check(window.CurrentCharacterFrame == CharacterFrame.Sit, "open menus suppress seated ambient reactions");
        window.EndDetailsMenu();
        window.AdvanceAmbient(TimeSpan.FromSeconds(16));
        check(window.CurrentCharacterFrame == CharacterFrame.SeatedHappy && window.CharacterResting,
            "settled unhovered host occasionally smiles while seated");
        window.HidePet();
        check(window.CurrentCharacterFrame == CharacterFrame.Idle && !window.CharacterRestPose,
            "hiding clears a seated response and its throne");
    }
}
