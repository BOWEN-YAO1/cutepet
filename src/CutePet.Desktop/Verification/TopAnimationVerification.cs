using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class TopAnimationVerification
{
    internal static void Run(string directory)
    {
        Directory.CreateDirectory(directory);var checks=new List<string>();
        void Check(bool passed,string label){if(!passed)throw new InvalidOperationException(label);checks.Add(label);}
        var pack=CharacterCatalog.BuiltIns.Single(p=>p.Id=="tianyi");
        var player=new CharacterAnimation();player.Configure(pack);
        var view=new NativeAnimationView();var texture=pack.Actions["edge-top-idle"].Frames[0].Image;
        var encoder=new GifBitmapEncoder();var delays=new List<int>();var captures=new List<BitmapSource>();
        var maximumMove=0.0;int startIris=0,closedIris=0;
        foreach(var action in EdgeActions.TopResponses)
        {
            player.Preview(action);view.Present(player);
            var start=WindowPreview.Surface(view,256,352,96);var startPixels=Pixels(start);
            Check(startPixels.Where((_,i)=>i%4==3).Count(a=>a>128)>20000,"top viewport paints actual transparent artwork "+action);
            Check(startPixels[3]==0,"top background remains transparent "+action);
            var stable=true;var pinned=true;var unfolded=true;var finite=true;
            var duration=pack.Actions[action].Duration;var previous=view.Mesh!.Positions.ToArray();
            if(action=="edge-top-peek")startIris=Iris(start);
            for(var step=0;step<Math.Ceiling(duration/(1000.0/60));step++)
            {
                var t=step*1000.0/60;view.Present(player);var points=view.Mesh!.Positions;
                stable&=ReferenceEquals(texture,view.Texture)&&ReferenceEquals(texture,player.Image);
                for(var i=0;i<points.Count;i++)
                {
                    maximumMove=Math.Max(maximumMove,(points[i]-previous[i]).Length);previous[i]=points[i];
                    finite&=double.IsFinite(points[i].X)&&double.IsFinite(points[i].Y);
                }
                for(var row=47;row<=51;row++)for(var column=0;column<=32;column++)
                {var p=points[row*33+column];pinned&=Math.Abs(p.X-column*8)<1e-8&&Math.Abs(p.Y-(352-row*352.0/72))<1e-8;}
                var triangles=view.Mesh.TriangleIndices;
                for(var i=0;i<triangles.Count;i+=3)
                {var a=points[triangles[i]];var b=points[triangles[i+1]];var c=points[triangles[i+2]];
                    unfolded&=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X)>0;}
                if(step%2==0){encoder.Frames.Add(BitmapFrame.Create(WindowPreview.Surface(view,144,198,96)));delays.Add(step%6==4?4:3);}
                if(t<duration/2&&t+1000.0/60>=duration/2)
                {
                    var middle=WindowPreview.Surface(view,256,352,96);captures.Add(start);captures.Add(middle);
                    if(action=="edge-top-peek")closedIris=Iris(middle);
                }
                player.Advance(TimeSpan.FromMilliseconds(Math.Min(1000.0/60,duration-t)));
            }
            Check(stable,"top motion retains exactly one body texture "+action);
            Check(pinned,"all vertices around both rope attachment points and jade board remain fixed "+action);
            Check(finite&&unfolded,"sampled head, eye and leg motion never folds the body mesh "+action);
            view.Present(player);
            Check(Pixels(WindowPreview.Surface(view,256,352,96)).SequenceEqual(startPixels),"top action ends at exact idle pixels "+action);
            var held=view.Mesh!.Positions;view.Present(player);
            Check(ReferenceEquals(held,view.Mesh.Positions),"unchanged top pose reuses the retained mesh "+action);
        }
        Check(startIris>50&&closedIris<startIris*.1,"independent eyelids actually cover the green irises: "+startIris+" -> "+closedIris);
        Check(maximumMove<3,"maximum vertex displacement below three source pixels per 60-Hz sample: "+maximumMove);
        var sheet=new DrawingVisual();using(var draw=sheet.RenderOpen())
            for(var i=0;i<captures.Count;i++)draw.DrawImage(captures[i],new Rect(i%2*256,i/2*352,256,352));
        var bitmap=new RenderTargetBitmap(512,1056,96,96,PixelFormats.Pbgra32);bitmap.Render(sheet);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(directory,"top-native-contact.png")))png.Save(file);
        using(var bytes=new MemoryStream()){encoder.Save(bytes);File.WriteAllBytes(Path.Combine(directory,"top-native-animation.gif"),ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(),delays));}
        var compact=TopAnimationClip.Deform(new Point(128,.44*352),new Size(256,352),
            pack.Manifest.TopAnimation! with {HeadPivotY=.35},new SideAnimationPose(1,1,1,0),.5);
        Check(double.IsFinite(compact.X)&&double.IsFinite(compact.Y),"compact custom seat registration keeps the head fade finite");
        view.Width=178;view.Height=148;view.Visibility=Visibility.Collapsed;
        player.Preview("edge-peek");view.Present(player);player.Preview("edge-top-smile");view.Present(player);
        Check(Math.Abs(((System.Windows.Media.Media3D.OrthographicCamera)view.Camera).Width-352.0*178/148)<1e-9,
            "hidden reused top viewport fits explicit image dimensions before first visible frame");
        view.Width=double.NaN;view.Height=double.NaN;view.Visibility=Visibility.Visible;
        player.Preview("edge-peek");Check(view.Present(player)&&view.Mesh!.Positions.Count==325,"switching top to side restores the existing side mesh");
        player.Configure(CharacterCatalog.BuiltIns.Single(p=>p.Id=="cat"));Check(!view.Present(player)&&view.Mesh is null,"legacy character clears native top resources");
        File.WriteAllText(Path.Combine(directory,"top-native-verification.json"),JsonSerializer.Serialize(new{passed=true,checks=checks.Count,maximumMove,startIris,closedIris,vertices=2409,triangles=4608},new JsonSerializerOptions{WriteIndented=true}));
    }
    private static byte[] Pixels(BitmapSource bitmap)
    {var source=new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);var bytes=new byte[source.PixelWidth*source.PixelHeight*4];source.CopyPixels(bytes,source.PixelWidth*4,0);return bytes;}
    private static int Iris(BitmapSource bitmap)
    {
        var bytes=Pixels(bitmap);var count=0;
        foreach(var (x,y,w,h) in new[]{(81,125,46,38),(136,122,48,38)})
            for(var yy=y;yy<y+h;yy++)for(var xx=x;xx<x+w;xx++)
            {var p=(yy*bitmap.PixelWidth+xx)*4;if(bytes[p+3]>200&&bytes[p+1]>bytes[p+2]*1.15&&bytes[p+1]>bytes[p]*1.05)count++;}
        return count;
    }
}
