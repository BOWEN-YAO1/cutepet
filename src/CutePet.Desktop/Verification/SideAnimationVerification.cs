using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class SideAnimationVerification
{
    internal static void Run(string directory)
    {
        Directory.CreateDirectory(directory);var checks=new List<string>();
        void Check(bool passed,string text){if(!passed)throw new InvalidOperationException(text);checks.Add(text);}
        var pack=CharacterCatalog.BuiltIns.Single(p=>p.Id=="tianyi");
        var player=new CharacterAnimation();player.Configure(pack);
        var view=new SideAnimationView();var texture=pack.Actions["edge-idle"].Frames[0].Image;
        var encoder=new GifBitmapEncoder();var contact=new DrawingVisual();var captures=new List<BitmapSource>();
        var previousPose=default(SideAnimationPose);var maximumDelta=0.0;
        Check(pack.Manifest.SideAnimation is not null,"native side animation is declared by the character pack");
        foreach(var action in SideEdgeMotion.Responses)
        {
            player.Preview(action);view.Present(player);
            var start=WindowPreview.Surface(view,288,384,96);var startPixels=Pixels(start);
            Check(startPixels.Where((_,i)=>i%4==3).Count(a=>a>128)>20000,"native transparent viewport actually renders the texture "+action);
            Check(startPixels[3]==0,"native animation background remains transparent "+action);
            previousPose=default;
            var fixedPalms=true;var stableTexture=true;var finite=true;var unfolded=true;
            var duration=pack.Actions[action].Duration;
            for(var t=0.0;t<duration;t+=1000.0/60)
            {
                view.Present(player);
                var pose=SideAnimationClip.Sample(pack.Manifest.SideAnimation!.Clips[action],player.ActionProgress);
                maximumDelta=Math.Max(maximumDelta,Math.Abs(pose.Peek-previousPose.Peek));previousPose=pose;
                var positions=view.Mesh!.Positions;
                stableTexture&=ReferenceEquals(texture,view.Texture)&&ReferenceEquals(texture,player.Image);
                finite&=positions.All(p=>double.IsFinite(p.X)&&double.IsFinite(p.Y));
                for(var row=13;row<=19;row++)for(var column=0;column<=3;column++)
                {var p=positions[row*13+column];fixedPalms&=Math.Abs(p.X-column*24)<.001&&Math.Abs(p.Y-(384-row*16))<.001;}
                var triangles=view.Mesh.TriangleIndices;
                for(var i=0;i<triangles.Count;i+=3)
                {
                    var a=positions[triangles[i]];var b=positions[triangles[i+1]];var c=positions[triangles[i+2]];
                    unfolded&=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X)>0;
                }
                if(((int)Math.Round(t/(1000.0/60)))%2==0)
                    encoder.Frames.Add(BitmapFrame.Create(WindowPreview.Surface(view,144,192,96)));
                if(t<duration/2&&t+1000.0/60>=duration/2)captures.Add(WindowPreview.Surface(view,288,384,96));
                player.Advance(TimeSpan.FromMilliseconds(Math.Min(1000.0/60,duration-t)));
            }
            Check(stableTexture,"one stable texture throughout "+action);
            Check(finite&&unfolded,"all sampled triangles remain finite and never fold "+action);
            Check(fixedPalms,"both palms stay fixed throughout "+action);
            view.Present(player);
            Check(Pixels(WindowPreview.Surface(view,288,384,96)).SequenceEqual(startPixels),"exact idle endpoint without residual pixels "+action);
            var held=view.Mesh!.Positions;
            view.Present(player);Check(ReferenceEquals(held,view.Mesh.Positions),"unchanged pose does not rebuild mesh "+action);
        }
        Check(maximumDelta<2,"head translation changes by fewer than two source pixels per 60-Hz sample: "+maximumDelta);
        using(var draw=contact.RenderOpen())for(var i=0;i<captures.Count;i++)draw.DrawImage(captures[i],new Rect(i*288,0,288,384));
        var sheet=new RenderTargetBitmap(1152,384,96,96,PixelFormats.Pbgra32);sheet.Render(contact);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(sheet));
        using(var file=File.Create(Path.Combine(directory,"side-native-contact.png")))png.Save(file);
        using(var bytes=new MemoryStream())
        {encoder.Save(bytes);File.WriteAllBytes(Path.Combine(directory,"side-native-animation.gif"),ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(),Enumerable.Repeat(3,encoder.Frames.Count).ToArray()));}
        var cat=CharacterCatalog.BuiltIns.Single(p=>p.Id=="cat");player.Configure(cat);
        Check(!view.Present(player)&&view.Mesh is null&&view.Texture is null,"switching to legacy role clears native texture and mesh");
        File.WriteAllText(Path.Combine(directory,"side-native-verification.json"),JsonSerializer.Serialize(new{passed=true,checks=checks.Count,maximumDelta,
            textureWidth=texture.PixelWidth,textureHeight=texture.PixelHeight,vertices=325,triangles=576},new JsonSerializerOptions{WriteIndented=true}));
    }
    private static byte[] Pixels(BitmapSource image)
    {var bytes=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(bytes,image.PixelWidth*4,0);return bytes;}
}
