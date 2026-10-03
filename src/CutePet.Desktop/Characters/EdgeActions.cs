using System;
using System.Collections.Generic;
using System.Linq;

namespace CutePet.Desktop;

internal static class EdgeActions
{
    internal static string Base(ScreenEdge side) => side switch
    { ScreenEdge.Top => "edge-top-idle", ScreenEdge.Bottom => "edge-bottom-idle", _ => "edge-idle" };
    internal static string Peek(string baseAction) => baseAction[..^4] + "peek";
    internal static string? BaseOf(string action) => action switch
    {
        "edge-idle" or "edge-peek" => "edge-idle",
        "edge-top-idle" or "edge-top-peek" => "edge-top-idle",
        "edge-bottom-idle" or "edge-bottom-peek" => "edge-bottom-idle",
        _ => null
    };
    internal static IEnumerable<ScreenEdge> Supported(CharacterPack pack) => Enum.GetValues<ScreenEdge>()
        .Where(side => pack.Actions.ContainsKey(Base(side)));
    internal static bool Available(CharacterPack pack) => Supported(pack).Any();
}
