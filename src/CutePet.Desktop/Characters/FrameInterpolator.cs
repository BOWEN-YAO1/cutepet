using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

// Blend only registered, package-opted-in frames. The two source buffers and one
// output are reused. Frozen source bytes share a bounded cache to avoid copying
// two large arrays on every refresh; mutable images never enter that cache.
internal sealed class FrameInterpolator
{
    private BitmapSource? first, second;
    private byte[]? firstPixels, secondPixels, output;
    private WriteableBitmap? bitmap;
    private int lastWeight = -1;
    private Vector lastDelta;
    private double lastGrip;
    internal const long MaxCachedPixelBytes = 80 * 1024 * 1024;
    private readonly Dictionary<BitmapSource,byte[]> pixelCache = new();
    private readonly Queue<BitmapSource> cacheOrder = new();
    internal long CachedPixelBytes { get; private set; }
    internal int SourceReadCount { get; private set; }

    internal BitmapSource Sample(LoadedFrame from, LoadedFrame to, double fraction)
    {
        var delta = from.HeadAnchorX is double x && from.HeadAnchorY is double y
            && to.HeadAnchorX is double tx && to.HeadAnchorY is double ty
            ? new Vector((tx-x)*from.Image.PixelWidth,(ty-y)*from.Image.PixelHeight) : default;
        // On a swing the fixed lower landmark is the seat, not the suspension
        // above the character. Never warp the board or its rope attachments.
        var grip=(from.SwingSeatAnchorY ?? from.EdgeAnchorY ?? 0)*from.Image.PixelHeight;
        // A side anchor is the midpoint between two palms. Stop head warping
        // above the upper palm so both hands remain attached to the border.
        if(from.SwingSeatAnchorY is null&&from.EdgeAnchorX is not null)
            grip=Math.Max(0,grip-from.Image.PixelHeight*.10);
        return Sample(from.Image,to.Image,fraction,delta,grip);
    }

    internal BitmapSource Sample(BitmapSource from, BitmapSource to, double fraction)
        => Sample(from,to,fraction,default,0);
    private BitmapSource Sample(BitmapSource from, BitmapSource to, double fraction, Vector delta, double grip)
    {
        if (ReferenceEquals(from, to) || fraction <= 0) return from;
        if (fraction >= 1) return to;
        if (from.PixelWidth != to.PixelWidth || from.PixelHeight != to.PixelHeight) return from;
        if (!ReferenceEquals(first, from) || !ReferenceEquals(second, to) || delta!=lastDelta || grip!=lastGrip)
        {
            var reuse = ReferenceEquals(second, from) ? secondPixels : null;
            firstPixels = reuse ?? Pixels(from);
            secondPixels = Pixels(to);
            first = from; second = to; lastWeight = -1;
            lastDelta=delta;lastGrip=grip;
        }
        var stride = from.PixelWidth * 4;
        if (bitmap is null || bitmap.PixelWidth != from.PixelWidth || bitmap.PixelHeight != from.PixelHeight)
        {
            bitmap = new WriteableBitmap(from.PixelWidth, from.PixelHeight, 96, 96, PixelFormats.Pbgra32, null);
            output = new byte[stride * from.PixelHeight]; lastWeight = -1;
        }
        var weight = (int)Math.Round(Math.Clamp(fraction, 0, 1) * 255);
        if (weight != lastWeight)
        {
            if(delta.LengthSquared<.000001)
            {
                for (var i = 0; i < output!.Length; i++)
                    output[i] = (byte)((firstPixels![i] * (255 - weight) + secondPixels![i] * weight + 127) / 255);
            }
            else
            {
                var width=from.PixelWidth;var height=from.PixelHeight;var t=weight/255.0;
                for(var y=0;y<height;y++)
                {
                    // Move the two head landmarks to their shared interpolated
                    // location, tapering to zero above the stationary hand grips.
                    var amount=Math.Clamp((grip-y)/64,0,1);
                    var a=new SampleRow(firstPixels!,width,height,-delta.X*t*amount,y-delta.Y*t*amount);
                    var b=new SampleRow(secondPixels!,width,height,delta.X*(1-t)*amount,y+delta.Y*(1-t)*amount);
                    for(var x=0;x<width;x++)for(var channel=0;channel<4;channel++)
                        output![(y*width+x)*4+channel]=(byte)((a.At(x,channel)*(255-weight)+b.At(x,channel)*weight+127)/255);
                }
            }
            bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), output, stride, 0);
            lastWeight = weight;
        }
        return bitmap;
    }

    internal readonly struct SampleRow
    {
        private readonly byte[] pixels;
        private readonly int width,height,xOffset,y0;
        private readonly int w00,w01,w10,w11;
        internal SampleRow(byte[] pixels,int width,int height,double offset,double y)
        {
            this.pixels=pixels;this.width=width;this.height=height;
            xOffset=(int)Math.Floor(offset);y0=(int)Math.Floor(y);
            var fx=offset-xOffset;var fy=y-y0;
            var ix=(int)Math.Round(fx*256);var iy=(int)Math.Round(fy*256);
            w00=(256-ix)*(256-iy);w01=ix*(256-iy);
            w10=(256-ix)*iy;w11=ix*iy;
        }
        internal int At(int x,int channel)
        {
            var sx=x+xOffset;
            if(sx>=0&&sx+1<width&&y0>=0&&y0+1<height)
            {
                var p=(y0*width+sx)*4+channel;var next=p+width*4;
                return (pixels[p]*w00+pixels[p+4]*w01+pixels[next]*w10+pixels[next+4]*w11+32768)>>16;
            }
            return (Get(sx,y0,channel)*w00+Get(sx+1,y0,channel)*w01
                +Get(sx,y0+1,channel)*w10+Get(sx+1,y0+1,channel)*w11+32768)>>16;
        }
        private byte Get(int x,int y,int channel)
            => x>=0&&x<width&&y>=0&&y<height?pixels[(y*width+x)*4+channel]:(byte)0;
    }

    private byte[] Pixels(BitmapSource source)
    {
        if(source.IsFrozen&&pixelCache.TryGetValue(source,out var cached))return cached;
        var image = source.Format == PixelFormats.Pbgra32 ? source
            : new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
        var stride = image.PixelWidth * 4;
        var bytes = new byte[stride * image.PixelHeight]; image.CopyPixels(bytes, stride, 0);
        SourceReadCount++;
        if(source.IsFrozen&&bytes.Length<=MaxCachedPixelBytes)
        {
            while(CachedPixelBytes+bytes.Length>MaxCachedPixelBytes&&cacheOrder.Count>0)
            {var oldest=cacheOrder.Dequeue();CachedPixelBytes-=pixelCache[oldest].Length;pixelCache.Remove(oldest);}
            pixelCache.Add(source,bytes);cacheOrder.Enqueue(source);CachedPixelBytes+=bytes.Length;
        }
        return bytes;
    }
    internal void Reset()
    { first = second = null; firstPixels = secondPixels = output = null; bitmap = null; lastWeight = -1;
        pixelCache.Clear();cacheOrder.Clear();CachedPixelBytes=0;SourceReadCount=0; }
}
