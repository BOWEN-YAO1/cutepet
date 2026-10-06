using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class TopAnimationClip
{
    internal static void Validate(CharacterManifest manifest,IReadOnlyDictionary<string,LoadedAction> actions,BitmapSource? closedEyes)
    {
        if(manifest.TopAnimation is not { } rig)return;
        if(manifest.TopSwing is not { } swing||!actions.TryGetValue("edge-top-idle",out var idle)
            ||!Bound(rig.HeadPivotX,.3,.7)||!Bound(rig.HeadPivotY,.35,.55)||rig.HeadPivotY>swing.SeatAnchorY-.12
            ||rig.Clips is null||rig.Clips.Count is <1 or >3||rig.Eyes is null||rig.Eyes.Count!=2||closedEyes is null)
            throw new InvalidDataException("连续上侧动画需要秋千、合法头部支点、两个眼部区域及闭眼图层。");
        var source=idle.Frames[0];
        if(closedEyes.PixelWidth!=source.Image.PixelWidth||closedEyes.PixelHeight!=source.Image.PixelHeight)
            throw new InvalidDataException("闭眼图层须与上侧基础贴图画布一致。");
        foreach(var eye in rig.Eyes)
            if(eye is null||eye.X<0||eye.Y<0||eye.Width<8||eye.Height<8
                ||eye.Width>source.Image.PixelWidth*.25||eye.Height>source.Image.PixelHeight*.15
                ||(long)eye.X+eye.Width>source.Image.PixelWidth||(long)eye.Y+eye.Height>source.Image.PixelHeight*.5)
                throw new InvalidDataException("眼部区域必须位于头部且在贴图边界内。");
        if(rig.Eyes[0].X+rig.Eyes[0].Width>=rig.Eyes[1].X)
            throw new InvalidDataException("左右眼部区域必须依次排列且不重叠。");
        foreach(var name in new[]{"edge-top-idle"}.Concat(EdgeActions.TopResponses.Where(actions.ContainsKey)))
        {
            var action=actions[name];var frame=action.Frames[0];
            if(action.Frames.Count!=1||action.SmoothFrames||manifest.Actions[name].Frames[0].Region is null
                ||source.HeadAnchorX is null||source.HeadAnchorY is null
                ||!ReferenceEquals(frame.Image,source.Image)||frame.EdgeAnchorY!=source.EdgeAnchorY
                ||frame.SwingSeatAnchorY!=swing.SeatAnchorY||frame.HeadAnchorX!=source.HeadAnchorX||frame.HeadAnchorY!=source.HeadAnchorY)
                throw new InvalidDataException("连续上侧动作共用一张固定座椅、悬挂点与头部登记的基础贴图。");
            if(name!="edge-top-idle"&&!rig.Clips.ContainsKey(name))throw new InvalidDataException("上侧动作缺少运动曲线。");
        }
        foreach(var (name,keys) in rig.Clips)
        {
            if(!EdgeActions.TopResponses.Contains(name)||!actions.ContainsKey(name)||keys is null||keys.Count is <2 or >32)
                throw new InvalidDataException("上侧曲线仅用于现有回应，每个动作2–32个控制点。");
            for(var i=0;i<keys.Count;i++)
            {
                var key=keys[i];
                if(key is null||!Bound(key.At,0,1)||i>0&&key.At<=keys[i-1].At
                    ||!Bound(key.Peek,-8,8)||!Bound(key.Lift,-5,5)||!Bound(key.Angle,-5,5)||!Bound(key.Sway,-3,3)||!Bound(key.Blink,0,1))
                    throw new InvalidDataException("上侧运动曲线时间必须递增，位移、角度与闭眼程度在允许范围内。");
            }
            if(keys[0]!=new CharacterMotionKey(0)||keys[^1]!=new CharacterMotionKey(1))
                throw new InvalidDataException("上侧运动曲线首尾必须准确回到基础姿态。");
        }
    }
    private static bool Bound(double value,double low,double high)=>double.IsFinite(value)&&value>=low&&value<=high;
    private static double Smooth(double value){value=Math.Clamp(value,0,1);return value*value*(3-2*value);}
    internal static Point Deform(Point source,Size size,CharacterTopAnimation rig,SideAnimationPose pose,double seat)
    {
        var x=source.X;var y=DeformEyes(source,rig.Eyes,pose.Blink);
        // The head influence ends before the jade board; all seat vertices stay fixed.
        var fadeStart=Math.Min(.44,seat-.12);
        var head=1-Smooth((source.Y/size.Height-fadeStart)/(seat-.06-fadeStart));
        var pivot=new Point(rig.HeadPivotX*size.Width,rig.HeadPivotY*size.Height);
        var dxHead=x-pivot.X;var dyHead=y-pivot.Y;var angle=pose.Angle*Math.PI/180;
        var rx=dxHead*(Math.Cos(angle)-1)-dyHead*Math.Sin(angle);
        var ry=dxHead*Math.Sin(angle)+dyHead*(Math.Cos(angle)-1);
        var hair=Smooth((Math.Abs(x/size.Width-.5)-.16)/.26);
        var legs=Smooth((source.Y/size.Height-.76)/.16)*(1-Smooth((Math.Abs(x/size.Width-.5)-.18)/.18));
        return new Point(x+(pose.Peek+rx+pose.Sway*hair)*head+pose.Sway*.45*legs,
            y+(pose.Lift+ry)*head+pose.Lift*.3*legs);
    }
    internal static double DeformEyes(Point source,IReadOnlyList<CharacterFrameRegion> eyes,double blink)
    {
        var y=source.Y;
        foreach(var eye in eyes)
        {
            var dx=source.X-eye.X-eye.Width/2.0;var dy=y-eye.Y-eye.Height/2.0;
            var weight=(1-Smooth((Math.Abs(dx)/(eye.Width/2.0)-.72)/.28))
                *(1-Smooth((Math.Abs(dy)/(eye.Height/2.0)-.7)/.3));
            y-=dy*.92*blink*weight;
        }
        return y;
    }
}
