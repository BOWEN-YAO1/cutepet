using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace CutePet.Desktop;

// A flat textured mesh is drawn by WPF's retained renderer. No per-frame PNG
// decoding, pixel blending, image replacement, or image sequence is involved.
public sealed class NativeAnimationView : Viewport3D
{
    private int columns=12,rows=24;
    private BitmapSource? texture;
    private MeshGeometry3D? mesh;
    private GeometryModel3D? model;
    private Point3DCollection? spare;
    private SideAnimationPose? previousPose;
    private Point[] rest=Array.Empty<Point>();
    private MeshGeometry3D[] eyes=Array.Empty<MeshGeometry3D>();
    private GeometryModel3D[] eyeModels=Array.Empty<GeometryModel3D>();
    private Brush[] eyeBrushes=Array.Empty<Brush>();
    private Point3DCollection[] eyeSpares=Array.Empty<Point3DCollection>();
    private Point[] eyeRest=Array.Empty<Point>();
    private readonly OrthographicCamera camera=new(){NearPlaneDistance=.1,FarPlaneDistance=2000};
    internal BitmapSource? Texture=>texture;
    internal MeshGeometry3D? Mesh=>mesh;
    internal bool Active=>texture is not null;
    public NativeAnimationView()
    {
        Camera=camera;IsHitTestVisible=false;ClipToBounds=false;
        SizeChanged+=(_,_)=>FitCamera();
    }
    internal bool Present(CharacterAnimation animation)
    {
        var side=animation.SideAnimation;var top=animation.TopAnimation;
        if(side is null&&top is null){Reset();return false;}
        var source=animation.SpriteFrame.Image;
        if(!ReferenceEquals(texture,source))Configure(source,animation);
        (side?.Clips??top!.Clips).TryGetValue(animation.Action,out var keys);
        var pose=SideAnimationClip.Sample(keys,animation.ActionProgress);
        FitCamera();
        if(previousPose==pose)return true;
        previousPose=pose;
        // Detach before modifying collections to avoid one scene invalidation
        // per vertex. Double-buffer the collections instead of allocating them.
        model!.Geometry=null;
        for(var i=0;i<rest.Length;i++)
        {
            var size=new Size(source.PixelWidth,source.PixelHeight);
            var point=top is null?SideAnimationClip.Deform(rest[i],size,side!,pose)
                :TopAnimationClip.Deform(rest[i],size,top,pose,animation.SwingSeat);
            spare![i]=new Point3D(point.X,source.PixelHeight-point.Y,0);
        }
        var old=mesh!.Positions;mesh.Positions=spare;spare=old;model.Geometry=mesh;
        if(top is not null)
        {
            for(var eye=0;eye<eyes.Length;eye++)
            {
                eyeModels[eye].Geometry=null;
                for(var j=0;j<4;j++)
                {
                    var p=TopAnimationClip.Deform(eyeRest[eye*4+j],new Size(source.PixelWidth,source.PixelHeight),top,pose with {Blink=0},animation.SwingSeat);
                    eyeSpares[eye][j]=new Point3D(p.X,source.PixelHeight-p.Y,.2);
                }
                var held=eyes[eye].Positions;eyes[eye].Positions=eyeSpares[eye];eyeSpares[eye]=held;eyeModels[eye].Geometry=eyes[eye];
                eyeBrushes[eye].Opacity=pose.Blink*pose.Blink*pose.Blink;
            }
        }
        return true;
    }
    private void Configure(BitmapSource source,CharacterAnimation animation)
    {
        Reset();texture=source;
        columns=animation.TopAnimation is null?12:32;rows=animation.TopAnimation is null?24:72;
        rest=new Point[(columns+1)*(rows+1)];
        var positions=new Point3DCollection(rest.Length);spare=new Point3DCollection(rest.Length);
        var coordinates=new PointCollection(rest.Length);var indices=new Int32Collection(columns*rows*6);
        for(var row=0;row<=rows;row++)for(var column=0;column<=columns;column++)
        {
            var i=row*(columns+1)+column;
            rest[i]=new Point(source.PixelWidth*(double)column/columns,source.PixelHeight*(double)row/rows);
            positions.Add(new Point3D(rest[i].X,source.PixelHeight-rest[i].Y,0));spare.Add(positions[i]);
            coordinates.Add(new Point((double)column/columns,(double)row/rows));
        }
        for(var row=0;row<rows;row++)for(var column=0;column<columns;column++)
        {
            var a=row*(columns+1)+column;var b=a+1;var c=a+columns+1;var d=c+1;
            indices.Add(a);indices.Add(c);indices.Add(b);indices.Add(b);indices.Add(c);indices.Add(d);
        }
        coordinates.Freeze();indices.Freeze();
        mesh=new MeshGeometry3D{Positions=positions,TextureCoordinates=coordinates,TriangleIndices=indices};
        var brush=new ImageBrush(source){Stretch=Stretch.Fill};brush.Freeze();
        var material=new DiffuseMaterial(brush);material.Freeze();
        model=new GeometryModel3D(mesh,material);
        var scene=new Model3DGroup();scene.Children.Add(new AmbientLight(Colors.White));scene.Children.Add(model);
        if(animation.TopAnimation is { } top)ConfigureEyes(scene,source,animation.TopClosedEyesImage!,top);
        Children.Add(new ModelVisual3D{Content=scene});
        camera.Position=new Point3D(source.PixelWidth/2.0,source.PixelHeight/2.0,1000);
        camera.LookDirection=new Vector3D(0,0,-1);camera.UpDirection=new Vector3D(0,1,0);FitCamera();
    }
    private void ConfigureEyes(Model3DGroup scene,BitmapSource source,BitmapSource closed,CharacterTopAnimation top)
    {
        eyes=new MeshGeometry3D[2];eyeModels=new GeometryModel3D[2];eyeBrushes=new Brush[2];
        eyeSpares=new Point3DCollection[2];eyeRest=new Point[8];
        for(var eye=0;eye<2;eye++)
        {
            var r=top.Eyes[eye];var points=new[]{new Point(r.X,r.Y),new Point(r.X+r.Width,r.Y),
                new Point(r.X,r.Y+r.Height),new Point(r.X+r.Width,r.Y+r.Height)};
            var positions=new Point3DCollection(4);eyeSpares[eye]=new Point3DCollection(4);
            for(var j=0;j<4;j++)
            {
                eyeRest[eye*4+j]=points[j];positions.Add(new Point3D(points[j].X,source.PixelHeight-points[j].Y,.2));eyeSpares[eye].Add(positions[j]);
            }
            var coordinates=new PointCollection(new[]{new Point(0,0),new Point(1,0),new Point(0,1),new Point(1,1)});
            var indices=new Int32Collection(new[]{0,2,1,1,2,3});coordinates.Freeze();indices.Freeze();
            eyes[eye]=new MeshGeometry3D{Positions=positions,TextureCoordinates=coordinates,TriangleIndices=indices};
            var crop=new CroppedBitmap(closed,new Int32Rect(r.X,r.Y,r.Width,r.Height));crop.Freeze();
            LinearGradientBrush Feather(bool vertical)
            {
                var amount=3.0/(vertical?r.Height:r.Width);
                var brush=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=vertical?new Point(0,1):new Point(1,0)};
                brush.GradientStops.Add(new GradientStop(Colors.Transparent,0));brush.GradientStops.Add(new GradientStop(Colors.White,amount));
                brush.GradientStops.Add(new GradientStop(Colors.White,1-amount));brush.GradientStops.Add(new GradientStop(Colors.Transparent,1));brush.Freeze();return brush;
            }
            var horizontal=new DrawingGroup{OpacityMask=Feather(false)};
            horizontal.Children.Add(new ImageDrawing(crop,new Rect(0,0,r.Width,r.Height)));
            var vertical=new DrawingGroup{OpacityMask=Feather(true)};vertical.Children.Add(horizontal);vertical.Freeze();
            eyeBrushes[eye]=new DrawingBrush(vertical){Stretch=Stretch.Fill,Opacity=0};
            eyeModels[eye]=new GeometryModel3D(eyes[eye],new DiffuseMaterial(eyeBrushes[eye]));scene.Children.Add(eyeModels[eye]);
        }
    }
    private void FitCamera()
    {
        if(texture is null)return;
        // Collapsed controls can report zero ActualSize while retaining their old
        // layout slot. Use the explicit image-bound dimensions before showing it.
        var width=double.IsFinite(Width)&&Width>0?Width:ActualWidth;
        var height=double.IsFinite(Height)&&Height>0?Height:ActualHeight;
        var aspect=width>0&&height>0?width/height:(double)texture.PixelWidth/texture.PixelHeight;
        var fitted=Math.Max(texture.PixelWidth,texture.PixelHeight*aspect);
        if(camera.Width!=fitted)camera.Width=fitted;
    }
    internal void Reset(){if(texture is null)return;Children.Clear();texture=null;mesh=null;model=null;spare=null;previousPose=null;eyes=Array.Empty<MeshGeometry3D>();eyeModels=Array.Empty<GeometryModel3D>();eyeBrushes=Array.Empty<Brush>();eyeSpares=Array.Empty<Point3DCollection>();eyeRest=Array.Empty<Point>();rest=Array.Empty<Point>();}
}
