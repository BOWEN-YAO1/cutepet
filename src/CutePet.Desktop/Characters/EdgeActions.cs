using System;
using System.Collections.Generic;
using System.Linq;

namespace CutePet.Desktop;

internal static class EdgeActions
{
    internal static readonly string[] TopResponses = { "edge-top-peek", "edge-top-look", "edge-top-smile" };
    internal static readonly string[] BottomResponses = { "edge-bottom-peek", "edge-bottom-look", "edge-bottom-smile" };
    internal static string Base(ScreenEdge side) => side switch
    { ScreenEdge.Top => "edge-top-idle", ScreenEdge.Bottom => "edge-bottom-idle", _ => "edge-idle" };
    internal static string Peek(string baseAction) => baseAction[..^4] + "peek";
    internal static string? BaseOf(string action) => action switch
    {
        "edge-idle" or "edge-peek" or "edge-shy" or "edge-sway" or "edge-nod" => "edge-idle",
        "edge-top-idle" or "edge-top-peek" or "edge-top-look" or "edge-top-smile" => "edge-top-idle",
        "edge-bottom-idle" or "edge-bottom-peek" or "edge-bottom-look" or "edge-bottom-smile" => "edge-bottom-idle",
        _ => null
    };
    internal static IEnumerable<ScreenEdge> Supported(CharacterPack pack) => Enum.GetValues<ScreenEdge>()
        .Where(side => pack.Actions.ContainsKey(Base(side)));
    internal static bool Available(CharacterPack pack) => Supported(pack).Any();
}
