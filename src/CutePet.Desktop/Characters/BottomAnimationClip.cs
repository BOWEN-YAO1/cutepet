using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class BottomAnimationClip
{
    internal static void Validate(CharacterManifest manifest,IReadOnlyDictionary<string,LoadedAction> actions,BitmapSource? closedEyes)
    {
        if(manifest.BottomAnimation is not { } rig)return;
        if(!actions.TryGetValue("edge-bottom-idle",out var idle)||!Bound(rig.HeadPivotX,.3,.7)||!Bound(rig.HeadPivotY,.55,.8)
            ||rig.Clips is null||rig.Clips.Count is <1 or >3||rig.Eyes is null||rig.Eyes.Count!=2||closedEyes is null)
            throw new InvalidDataException("连续下侧动画需要托腮基础姿态、头部支点、两个眼部区域和闭眼图层。");
        var source=idle.Frames[0];var support=source.EdgeAnchorY;
        if(support is null||support<.85||rig.HeadPivotY>support-.12
            ||closedEyes.PixelWidth!=source.Image.PixelWidth||closedEyes.PixelHeight!=source.Image.PixelHeight)
            throw new InvalidDataException("下侧支撑点必须在画布下部，闭眼图层须与基础贴图画布一致。");
        foreach(var eye in rig.Eyes)
            if(eye is null||eye.X<0||eye.Y<0||eye.Width<8||eye.Height<8||eye.Width>source.Image.PixelWidth*.25
                ||eye.Height>source.Image.PixelHeight*.16||(long)eye.X+eye.Width>source.Image.PixelWidth
                ||(long)eye.Y+eye.Height>source.Image.PixelHeight*(rig.HeadPivotY-.02))
                throw new InvalidDataException("下侧眼部区域必须完整位于头部且在贴图边界内。");
        if(rig.Eyes[0].X+rig.Eyes[0].Width>=rig.Eyes[1].X)
            throw new InvalidDataException("下侧左右眼部区域必须依次排列且不重叠。");
        foreach(var name in new[]{"edge-bottom-idle"}.Concat(EdgeActions.BottomResponses.Where(actions.ContainsKey)))
        {
            var action=actions[name];var frame=action.Frames[0];
            if(action.Frames.Count!=1||action.SmoothFrames||manifest.Actions[name].Frames[0].Region is null
                ||!ReferenceEquals(frame.Image,source.Image)||frame.EdgeAnchorY!=support)
                throw new InvalidDataException("连续下侧动作须共用一张固定袖口支撑点的基础贴图。");
            if(name!="edge-bottom-idle"&&!rig.Clips.ContainsKey(name))throw new InvalidDataException("下侧回应缺少运动曲线。");
        }
        foreach(var (name,keys) in rig.Clips)
        {
            if(!EdgeActions.BottomResponses.Contains(name)||!actions.ContainsKey(name)||keys is null||keys.Count is <2 or >32)
                throw new InvalidDataException("下侧曲线仅用于现有回应，每个动作2–32个控制点。");
            for(var i=0;i<keys.Count;i++)
            {
                var key=keys[i];
                if(key is null||!Bound(key.At,0,1)||i>0&&key.At<=keys[i-1].At||!Bound(key.Peek,-6,6)
                    ||!Bound(key.Lift,-5,5)||!Bound(key.Angle,-4,4)||!Bound(key.Sway,-2,2)||!Bound(key.Blink,0,1))
                    throw new InvalidDataException("下侧曲线须时间递增，位移、角度与闭眼程度在允许范围内。");
            }
            if(keys[0]!=new CharacterMotionKey(0)||keys[^1]!=new CharacterMotionKey(1))
                throw new InvalidDataException("下侧运动曲线首尾必须准确回到托腮基础姿态。");
        }
    }
    private static bool Bound(double value,double low,double high)=>double.IsFinite(value)&&value>=low&&value<=high;
    private static double Smooth(double value){value=Math.Clamp(value,0,1);return value*value*(3-2*value);}
    internal static Point Deform(Point source,Size size,CharacterBottomAnimation rig,SideAnimationPose pose,double support)
    {
        var x=source.X;var y=TopAnimationClip.DeformEyes(source,rig.Eyes,pose.Blink);
        // Head and cupping hands move together, fading through the forearms.
        // The final support band, including both sleeve contact points, is pinned.
        var end=support-.04;var start=Math.Min(rig.HeadPivotY-.02,end-.12);
        var head=1-Smooth((source.Y/size.Height-start)/(end-start));
        var dx=x-rig.HeadPivotX*size.Width;var dy=y-rig.HeadPivotY*size.Height;
        var angle=pose.Angle*Math.PI/180;
        var rx=dx*(Math.Cos(angle)-1)-dy*Math.Sin(angle);
        var ry=dx*Math.Sin(angle)+dy*(Math.Cos(angle)-1);
        var hair=Smooth((Math.Abs(x/size.Width-.5)-.2)/.22);
        return new Point(x+(pose.Peek+rx+pose.Sway*hair)*head,y+(pose.Lift+ry)*head);
    }
}
