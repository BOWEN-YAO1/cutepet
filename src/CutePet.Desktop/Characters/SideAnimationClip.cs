using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace CutePet.Desktop;

internal readonly record struct SideAnimationPose(double Peek,double Lift,double Angle,double Sway);

internal static class SideAnimationClip
{
    internal static void Validate(CharacterManifest manifest,IReadOnlyDictionary<string,LoadedAction> actions)
    {
        if(manifest.SideAnimation is not { } rig)return;
        if(!double.IsFinite(rig.IdleOffsetX)||rig.IdleOffsetX < -48||rig.IdleOffsetX>0
            ||!double.IsFinite(rig.HeadPivotX)||rig.HeadPivotX<.25||rig.HeadPivotX>.8
            ||!double.IsFinite(rig.HeadPivotY)||rig.HeadPivotY<.35||rig.HeadPivotY>.65
            ||rig.Clips is null||rig.Clips.Count is <1 or >4||!actions.TryGetValue("edge-idle",out var idle))
            throw new InvalidDataException("连续侧边动画需要合法支点、位移和左右贴边基础姿势。");
        var source=idle.Frames[0];
        foreach(var name in new[]{"edge-idle"}.Concat(SideEdgeMotion.Responses.Where(actions.ContainsKey)))
        {
            var action=actions[name];
            if(action.Frames.Count!=1||action.SmoothFrames||manifest.Actions[name].Frames[0].Region is null
                ||source.EdgeAnchorX is null||source.EdgeAnchorY is null
                ||!ReferenceEquals(action.Frames[0].Image,source.Image)
                ||action.Frames[0].EdgeAnchorX!=source.EdgeAnchorX||action.Frames[0].EdgeAnchorY!=source.EdgeAnchorY)
                throw new InvalidDataException("连续侧边动作共用一张相同画布、固定双手的基础贴图，不能混合序列图。");
            if(name!="edge-idle"&&!rig.Clips.ContainsKey(name))throw new InvalidDataException("侧边动作缺少连续运动曲线。");
        }
        foreach(var (name,keys) in rig.Clips)
        {
            if(!SideEdgeMotion.Responses.Contains(name)||!actions.ContainsKey(name)||keys is null||keys.Count is <2 or >32)
                throw new InvalidDataException("连续运动曲线仅用于已存在的左右回应，每个动作2–32个控制点。");
            for(var i=0;i<keys.Count;i++)
            {
                var key=keys[i];
                if(key is null||!double.IsFinite(key.At)||key.At<0||key.At>1||i>0&&key.At<=keys[i-1].At
                    ||!Bound(key.Peek,-16,40)||!Bound(key.Lift,-8,8)||!Bound(key.Angle,-6,6)||!Bound(key.Sway,-4,4))
                    throw new InvalidDataException("运动曲线时间必须递增且位移、角度在允许范围内。");
            }
            if(keys[0].At!=0||keys[^1].At!=1||keys[0]!=new CharacterMotionKey(0)||keys[^1]!=new CharacterMotionKey(1))
                throw new InvalidDataException("运动曲线从0到1，开始和结束必须回到基础姿势。");
        }
    }
    private static bool Bound(double value,double low,double high)=>double.IsFinite(value)&&value>=low&&value<=high;
    internal static SideAnimationPose Sample(IReadOnlyList<CharacterMotionKey>? keys,double progress)
    {
        if(keys is null)return default;
        var t=Math.Clamp(progress,0,1);var i=0;
        while(i+2<keys.Count&&t>keys[i+1].At)i++;
        return new(Curve(keys,i,t,k=>k.Peek),Curve(keys,i,t,k=>k.Lift),
            Curve(keys,i,t,k=>k.Angle),Curve(keys,i,t,k=>k.Sway));
    }
    // Shape-preserving cubic Hermite: continuous velocity, no overshoot into the border.
    private static double Curve(IReadOnlyList<CharacterMotionKey> keys,int i,double at,Func<CharacterMotionKey,double> value)
    {
        double Tangent(int j)
        {
            if(j==0||j==keys.Count-1)return 0;
            var before=keys[j].At-keys[j-1].At;var after=keys[j+1].At-keys[j].At;
            var a=(value(keys[j])-value(keys[j-1]))/before;var b=(value(keys[j+1])-value(keys[j]))/after;
            if(a*b<=0)return 0;
            var w1=2*after+before;var w2=after+2*before;return(w1+w2)/(w1/a+w2/b);
        }
        var span=keys[i+1].At-keys[i].At;var u=(at-keys[i].At)/span;var u2=u*u;var u3=u2*u;
        return(2*u3-3*u2+1)*value(keys[i])+(u3-2*u2+u)*span*Tangent(i)
            +(-2*u3+3*u2)*value(keys[i+1])+(u3-u2)*span*Tangent(i+1);
    }
    internal static Point Deform(Point source,Size size,CharacterSideAnimation rig,SideAnimationPose pose)
    {
        var x=source.X;var y=source.Y;
        double Smooth(double a){a=Math.Clamp(a,0,1);return a*a*(3-2*a);}
        // Both painted palms remain fixed in this registered texture's grip area.
        var pin=1-(1-Smooth((x/size.Width-.27)/.36))*Smooth((y/size.Height-.50)/.04)
            *(1-Smooth((y/size.Height-.8125)/.125));
        var head=1-Smooth((y/size.Height-.52)/.29);
        var body=(.58+.42*head)*pin;
        var center=new Point(rig.HeadPivotX*size.Width,rig.HeadPivotY*size.Height);
        var angle=pose.Angle*Math.PI/180;var dx=x-center.X;var dy=y-center.Y;
        var rx=dx*(Math.Cos(angle)-1)-dy*Math.Sin(angle);var ry=dx*Math.Sin(angle)+dy*(Math.Cos(angle)-1);
        return new Point(x+(rig.IdleOffsetX+pose.Peek)*body+rx*head*pin
            +pose.Sway*(1-head)*pin,y+(pose.Lift+ry)*head*pin);
    }
}
