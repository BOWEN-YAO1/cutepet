using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CutePet.Desktop;

// Isolated measurement of real registered drawings, with no desktop window.
internal static class AnimationPerformanceVerification
{
    internal static void Run(string path)
    {
        var pack=CharacterCatalog.BuiltIns.Single(p=>p.Id=="tianyi");
        var player=new CharacterAnimation();player.Configure(pack);
        var renderer=new CharacterFrameRenderer();
        var times=new System.Collections.Generic.List<double>();
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        foreach(var action in SideEdgeMotion.Responses)
        {
            player.Preview(action);
            for(var elapsed=0.0;elapsed<pack.Actions[action].Duration;elapsed+=1000.0/60)
            {
                var started=Stopwatch.GetTimestamp();
                renderer.Render(player);
                times.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
                player.Advance(TimeSpan.FromMilliseconds(1000.0/60));
            }
        }
        var sorted=times.Order().ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path,JsonSerializer.Serialize(new {frames=times.Count,
            meanMs=times.Average(),p95Ms=sorted[(int)(sorted.Length*.95)],
            maxMs=times.Max(),overBudget=times.Count(t=>t>1000.0/60),
            allocatedMiB=(GC.GetAllocatedBytesForCurrentThread()-allocated)/1048576.0,
            sourceReads=renderer.SourceReadCount,cacheMiB=renderer.CachedPixelBytes/1048576.0},
            new JsonSerializerOptions{WriteIndented=true}));
    }
}
