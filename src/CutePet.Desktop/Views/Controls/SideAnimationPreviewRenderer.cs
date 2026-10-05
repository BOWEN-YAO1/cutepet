using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

// The scrolling 2D character page receives a small reusable transparent surface.
// Motion still comes from the same native mesh; there is no bitmap sequence.
internal sealed class SideAnimationPreviewRenderer
{
    internal SideAnimationView View { get; } = new();
    private RenderTargetBitmap? target;
    private WriteableBitmap? output;
    private byte[]? pixels;
    private double elapsed=1000;
    private string? action;
    internal void Advance(TimeSpan delta)=>elapsed+=Math.Max(0,delta.TotalMilliseconds);
    internal BitmapSource? Present(CharacterAnimation player)
    {
        if(!View.Present(player)){Reset();return null;}
        if(output is null)
        {
            View.Measure(new Size(144,192));View.Arrange(new Rect(0,0,144,192));View.UpdateLayout();
            target=new RenderTargetBitmap(144,192,96,96,PixelFormats.Pbgra32);
            output=new WriteableBitmap(144,192,96,96,PixelFormats.Pbgra32,null);pixels=new byte[144*192*4];
        }
        if(elapsed>=1000.0/30||action!=player.Action)
        {
            target!.Clear();target.Render(View);target.CopyPixels(pixels!,144*4,0);
            output.WritePixels(new Int32Rect(0,0,144,192),pixels!,144*4,0);
            elapsed=0;action=player.Action;
        }
        return output;
    }
    internal void Reset()
    {View.Reset();target=null;output=null;pixels=null;elapsed=1000;action=null;}
}
