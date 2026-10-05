using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

// Desktop and control-center previews use exactly the same presentation path.
internal sealed class CharacterFrameRenderer
{
    private readonly FrameInterpolator interpolator=new();
    private readonly SideFrameSmoother smoother=new();
    internal long CachedPixelBytes=>interpolator.CachedPixelBytes;
    internal int SourceReadCount=>interpolator.SourceReadCount;
    internal BitmapSource Render(CharacterAnimation animation)
    {
        var sample=animation.Presentation;
        var image=interpolator.Sample(sample.From,sample.To,sample.Fraction);
        if(animation.SmoothFrames&&Array.IndexOf(SideEdgeMotion.Responses,animation.Action)>=0
            &&sample.From.HeadAnchorX is double x&&sample.From.HeadAnchorY is double y
            &&sample.To.HeadAnchorX is double tx&&sample.To.HeadAnchorY is double ty
            &&sample.From.EdgeAnchorX.HasValue&&sample.From.EdgeAnchorY is double grip)
        {
            var head=new Point((x+(tx-x)*sample.Fraction)*image.PixelWidth,
                (y+(ty-y)*sample.Fraction)*image.PixelHeight);
            return smoother.Sample(image,head,Math.Max(0,(grip-.10)*image.PixelHeight),
                animation.PresentationTime,animation.RemainingTime,animation.Action);
        }
        smoother.Reset();return image;
    }
    internal void Reset(){interpolator.Reset();smoother.Reset();}
}

// A short, time-based filter aligns the current drawing and previous output at
// one continuous head location. Hands stay fixed. It suppresses ten-ms texture
// and landmark jitter without retaining an unbounded intermediate frame library.
internal sealed class SideFrameSmoother
{
    private byte[]? input,history,output;
    private WriteableBitmap? bitmap;
    private Point previousHead;
    private double previousTime;
    private string? previousAction;
    internal BitmapSource Sample(BitmapSource image,Point head,double grip,double time,double remaining,string action)
    {
        var reset=bitmap is null||bitmap.PixelWidth!=image.PixelWidth||bitmap.PixelHeight!=image.PixelHeight
            ||previousAction!=action||time<previousTime;
        if(!reset&&time==previousTime)return bitmap!;
        var width=image.PixelWidth;var height=image.PixelHeight;var stride=width*4;
        if(reset)
        {
            input=new byte[stride*height];history=new byte[input.Length];output=new byte[input.Length];
            bitmap=new WriteableBitmap(width,height,96,96,PixelFormats.Pbgra32,null);
        }
        var source=image.Format==PixelFormats.Pbgra32?image:new FormatConvertedBitmap(image,PixelFormats.Pbgra32,null,0);
        source.CopyPixels(input!,stride,0);
        if(reset)
        {input!.CopyTo(history!,0);previousHead=head;}
        else
        {
            // Drain history before the exact idle endpoint, preventing an exit snap.
            var tau=Math.Min(35,Math.Max(0,remaining/3));
            var alpha=tau<.01?1:1-Math.Exp(-(time-previousTime)/tau);
            var target=previousHead+(head-previousHead)*alpha;
            var currentOffset=head-target;var pastOffset=previousHead-target;
            var weight=(int)Math.Round(alpha*255);
            for(var y=0;y<height;y++)
            {
                var amount=Math.Clamp((grip-y)/64,0,1);
                var current=new FrameInterpolator.SampleRow(input!,width,height,currentOffset.X*amount,y+currentOffset.Y*amount);
                var past=new FrameInterpolator.SampleRow(history!,width,height,pastOffset.X*amount,y+pastOffset.Y*amount);
                for(var x=0;x<width;x++)for(var c=0;c<4;c++)
                    output![(y*width+x)*4+c]=(byte)((current.At(x,c)*weight+past.At(x,c)*(255-weight)+127)/255);
            }
            (history,output)=(output,history);previousHead=target;
        }
        previousAction=action;previousTime=time;
        bitmap!.WritePixels(new Int32Rect(0,0,width,height),history!,stride,0);return bitmap;
    }
    internal void Reset(){input=history=output=null;bitmap=null;previousAction=null;previousTime=0;}
}
