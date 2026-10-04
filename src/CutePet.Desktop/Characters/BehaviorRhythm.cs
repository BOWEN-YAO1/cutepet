using System;
using System.Collections.Generic;
using System.Linq;

namespace CutePet.Desktop;

internal enum QuietActivity { None, Rest, Cloud }

// One plan for substantial automatic activity. It measures quiet time, not wall time.
internal sealed class BehaviorRhythm
{
    private readonly Func<double> random;
    private readonly Dictionary<string, (List<string> Bag, string? Last)> choices = new();
    private QuietActivity last;
    internal QuietActivity Next { get; private set; }
    internal double RemainingMs { get; private set; }
    internal BehaviorRhythm(Func<double>? random = null) => this.random = random ?? Random.Shared.NextDouble;
    internal double Between(double minimum, double maximum) => minimum + (maximum-minimum) * Math.Clamp(random(), 0, .999999);
    internal void Plan(bool rest, bool cloud, int restAfterMs)
    {
        if (Next == QuietActivity.Rest && rest || Next == QuietActivity.Cloud && cloud) return;
        var available = new List<QuietActivity>();
        if (rest) available.Add(QuietActivity.Rest);
        if (cloud) available.Add(QuietActivity.Cloud);
        if (available.Count > 1) available.Remove(last);
        Next = available.Count == 0 ? QuietActivity.None : available[(int)Between(0, available.Count)];
        RemainingMs = Next switch
        { QuietActivity.Rest => Between(restAfterMs * .85, restAfterMs * 1.25), QuietActivity.Cloud => Between(25000, 50000), _ => 0 };
    }
    internal QuietActivity Advance(TimeSpan elapsed)
    {
        if (Next == QuietActivity.None) return QuietActivity.None;
        RemainingMs = Math.Max(0, RemainingMs - Math.Max(0, elapsed.TotalMilliseconds));
        return RemainingMs == 0 ? Next : QuietActivity.None;
    }
    internal void Started(QuietActivity activity) { last = activity; ResetQuiet(); }
    internal void ResetQuiet() { Next = QuietActivity.None; RemainingMs = 0; }
    internal void Reset() { last = QuietActivity.None; choices.Clear(); ResetQuiet(); }
    internal string Choose(string group, IEnumerable<string> candidates)
    {
        var available = candidates.Distinct().ToArray();
        if (available.Length == 0) throw new ArgumentException("An ambient choice needs at least one action.");
        choices.TryGetValue(group, out var state);
        var bag = state.Bag;
        if (bag is null || bag.Count == 0 || bag.Any(item => !available.Contains(item))) bag = available.ToList();
        var nonRepeating = bag.Where(item => item != state.Last).ToArray();
        var selected = (nonRepeating.Length > 0 ? nonRepeating : bag.ToArray())[(int)Between(0, nonRepeating.Length > 0 ? nonRepeating.Length : bag.Count)];
        bag.Remove(selected); choices[group] = (bag, selected);
        return selected;
    }
}
