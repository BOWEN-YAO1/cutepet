using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class SequenceVerification
{
    internal static void Run(CharacterPack pack, string directory, Action<bool,string> check)
    {
        var unique = pack.Actions.Values.SelectMany(c=>c.Frames).DistinctBy(f=>f.Image).ToArray();
        check(unique.Length==354 && unique.All(f=>f.Image.IsFrozen),
            "registered standing poses and a replacement side sequence share frozen caches");
        var hashes=new HashSet<string>();
        foreach(var frame in unique)
        {
            var rgba=new FormatConvertedBitmap(frame.Image,PixelFormats.Bgra32,null,0);
            var stride=rgba.PixelWidth*4;var bytes=new byte[stride*rgba.PixelHeight];rgba.CopyPixels(bytes,stride,0);
            hashes.Add(Convert.ToHexString(SHA256.HashData(bytes)));
            check(Enumerable.Range(0,rgba.PixelWidth).All(x=>bytes[x*4+3]<200
                && bytes[(rgba.PixelHeight-1)*stride+x*4+3]<200),
                "registered pose preserves vertical transparent gutters "+hashes.Count);
        }
        check(hashes.Count==354,"adopted poses contain distinct painted pixels rather than duplicate stills");
        check(pack.Manifest.Actions.Values.SelectMany(a=>a.Frames).Where(f=>f.Canvas is not null && f.Image.Contains("inbetweens"))
            .DistinctBy(f=>(f.Image,f.Region,f.Canvas)).Count()==50,
            "remaining insertion sources have explicit scale and placement registration");
        check(ReferenceEquals(pack.Actions["greeting"].Frames[0].Image,pack.Idle.Frames[0].Image)
            && ReferenceEquals(pack.Actions["happy"].Frames[0].Image,pack.Idle.Frames[0].Image),
            "standing responses enter from the exact shared idle pose");
        var reveal=pack.Actions["edge-peek"].Frames.Where(f=>f.Image is RenderTargetBitmap).Select(f=>f.Image).ToArray();
        check(reveal.Length==287 && reveal.Take(144).SequenceEqual(reveal.TakeLast(144).Reverse()),
            "side reveal uses 144 registered drawings and exactly reverses them on return");
        check(pack.Actions["greeting"].Duration==880 && pack.Actions["conjure"].Duration==1500
            && pack.Actions["stand"].Duration==1000 && pack.Actions["cloud-idle"].Duration==1260,
            "extra drawings preserve wave, throne and cruising durations");
        var player=new CharacterAnimation();player.Configure(pack);
        player.Flying=true; var start=player.Image;
        player.Advance(TimeSpan.FromMilliseconds(180));
        check(player.Action=="cloud-idle" && player.Image!=start,"cruising advances drawn hair and skirt poses");
        player.Blink();player.Advance(TimeSpan.FromMilliseconds(pack.Actions["cloud-blink"].Duration));
        check(player.Action=="cloud-idle" && player.Flying,"cloud blink returns to the cruising base");
        player.Low=true;
        check(player.Action=="low" && !player.Flying,"fresh low quota clears the cruising pose");
        player.Configure(pack);player.Preview("cloud-blink");player.Flying=false;
        check(player.Action=="idle","landing clears an unfinished cruising blink");
        player.Preview("cloud-idle");player.Reset();
        check(player.Action=="idle" && !player.Flying,"reset clears a looping cloud preview");

        // Preview the package player's real chronological sprites. WPF renders the
        // cropped sources without modifying the original PNGs.
        var encoder=new GifBitmapEncoder();var delays=new List<int>();var samples=new List<BitmapSource>();
        foreach(var (action,label) in new[] {("greeting","挥手"),("happy","开心"),("conjure","召唤与坐下"),
            ("sit-greeting","坐姿挥手"),("cloud-idle","乘云随风")})
        {
            player.Configure(pack);player.Preview(action);
            var clip=pack.Actions[action];
            var index=0;
            foreach(var frame in clip.Frames)
            {
                var visual=new DrawingVisual();using(var draw=visual.RenderOpen())
                {
                    draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(248,242,246)),null,new Rect(0,0,260,290));
                    draw.DrawText(new FormattedText(label,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,
                        new Typeface("Microsoft YaHei"),16,new SolidColorBrush(Color.FromRgb(102,75,98)),1),new Point(20,12));
                    var height=239.0;var width=height*player.Image.PixelWidth/player.Image.PixelHeight;
                    draw.DrawImage(player.Image,new Rect((260-width)/2,40,width,height));
                }
                var bitmap=new RenderTargetBitmap(260,290,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
                encoder.Frames.Add(BitmapFrame.Create(bitmap));delays.Add(Math.Max(4,frame.DurationMs/10));
                if(index==0 || index==clip.Frames.Count/3 || index==clip.Frames.Count*2/3 || index==clip.Frames.Count-1) samples.Add(bitmap);
                index++;
                player.Advance(TimeSpan.FromMilliseconds(frame.DurationMs));
            }
        }
        using var output=new MemoryStream();encoder.Save(output);
        File.WriteAllBytes(Path.Combine(directory,"pose-sequences.gif"),ThroneMotionVerification.WithAnimationMetadata(output.ToArray(),delays));
        var sheet=new DrawingVisual();using(var draw=sheet.RenderOpen())
            for(var i=0;i<samples.Count;i++)draw.DrawImage(samples[i],new Rect(i%4*260,i/4*290,260,290));
        var contactSheet=new RenderTargetBitmap(1040,1450,96,96,PixelFormats.Pbgra32);contactSheet.Render(sheet);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(contactSheet));
        using var contactFile=File.Create(Path.Combine(directory,"pose-contact-sheet.png"));png.Save(contactFile);
    }
}
