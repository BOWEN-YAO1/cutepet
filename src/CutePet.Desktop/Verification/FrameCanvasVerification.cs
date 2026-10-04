using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class FrameCanvasVerification
{
    internal static void Run(Action<bool,string> check)
    {
        var pixels = Enumerable.Range(0,64).SelectMany(_=>new byte[]{0,0,255,255}).ToArray();
        var source = BitmapSource.Create(8,8,96,96,PixelFormats.Bgra32,null,pixels,32);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(source));
        using var png=new MemoryStream();encoder.Save(png);var bytes=png.ToArray();
        var frame=new CharacterActionFrame("source.png",80) {Region=new(0,0,8,8),Canvas=new(64,64,2,8,12)};
        CharacterPack Load(params CharacterActionFrame[] frames)
        {
            var manifest=new CharacterManifest {Id="registration-test",Name="登记测试",
                Actions=new() { ["idle"]=new() {Loop=true,Frames=frames.ToList()} }};
            using var json=new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(manifest,CharacterPackLoader.Json));
            return CharacterPackLoader.Load(json,_=>new MemoryStream(bytes),false);
        }
        var pack=Load(frame,frame);var image=pack.Idle.Frames[0].Image;
        var actual=new byte[64*64*4];image.CopyPixels(actual,64*4,0);
        check(image is RenderTargetBitmap {IsFrozen:true,PixelWidth:64,PixelHeight:64}
            && ReferenceEquals(image,pack.Idle.Frames[1].Image),"registered canvas renders once and shares its frozen cache");
        check(actual[(16*64+12)*4+2]==255 && actual[(16*64+12)*4+3]==255
            && actual[(2*64+2)*4+3]==0 && actual[(40*64+40)*4+3]==0,
            "uniform registration preserves source color and transparent space at its declared placement");
        foreach(var invalid in new[] {new CharacterFrameCanvas(0,64,1,0,0),new(2049,64,1,0,0),
            new(64,64,0,0,0),new(64,64,5,0,0),
            new(64,64,1,-1,0),new(64,64,1,60,0)})
            Reject(()=>Load(frame with {Canvas=invalid}),"invalid registration is rejected "+invalid);
        foreach(var number in new[]{"1e400","-1e400"})
            Reject(()=>CharacterPackLoader.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                "{\"id\":\"overflow-test\",\"name\":\"test\",\"actions\":{\"idle\":{\"loop\":true,\"frames\":["
                +"{\"image\":\"source.png\",\"durationMs\":80,\"region\":{\"x\":0,\"y\":0,\"width\":8,\"height\":8},"
                +"\"canvas\":{\"width\":64,\"height\":64,\"scale\":"+number+",\"offsetX\":0,\"offsetY\":0}}]}}}")),
                _=>new MemoryStream(bytes),false),"overflowed numeric registration cannot load "+number);
        Reject(()=>Load(frame with {Region=null}),"canvas cannot be used without a source region");
        Reject(()=>Load(Enumerable.Range(0,12).Select(i=>frame with {Canvas=new(2048,2048,1,i,0)}).ToArray()),
            "distinct rendered canvases count toward the shared decoded-pixel budget");
        void Reject(Action action,string description)
        {
            var rejected=false;try {action();}catch(InvalidDataException){rejected=true;}catch(JsonException){rejected=true;}
            check(rejected,description);
        }
    }
}
