using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace CutePet.Desktop;

// A flat textured mesh is drawn by WPF's retained renderer. No per-frame PNG
// decoding, pixel blending, image replacement, or image sequence is involved.
public sealed class SideAnimationView : Viewport3D
{
    private const int Columns=12,Rows=24;
    private BitmapSource? texture;
    private MeshGeometry3D? mesh;
    private GeometryModel3D? model;
    private Point3DCollection? spare;
    private SideAnimationPose? previousPose;
    private readonly Point[] rest=new Point[(Columns+1)*(Rows+1)];
    private readonly OrthographicCamera camera=new(){NearPlaneDistance=.1,FarPlaneDistance=2000};
    internal BitmapSource? Texture=>texture;
    internal MeshGeometry3D? Mesh=>mesh;
    internal bool Active=>texture is not null;
    public SideAnimationView()
    {
        Camera=camera;IsHitTestVisible=false;ClipToBounds=false;
        SizeChanged+=(_,_)=>FitCamera();
    }
    internal bool Present(CharacterAnimation animation)
    {
        if(animation.SideAnimation is not { } rig){Reset();return false;}
        var source=animation.SpriteFrame.Image;
        if(!ReferenceEquals(texture,source))Configure(source);
        rig.Clips.TryGetValue(animation.Action,out var keys);
        var pose=SideAnimationClip.Sample(keys,animation.ActionProgress);
        if(previousPose==pose)return true;
        previousPose=pose;
        // Detach before modifying collections to avoid one scene invalidation
        // per vertex. Double-buffer the collections instead of allocating them.
        model!.Geometry=null;
        for(var i=0;i<rest.Length;i++)
        {
            var point=SideAnimationClip.Deform(rest[i],new Size(source.PixelWidth,source.PixelHeight),rig,pose);
            spare![i]=new Point3D(point.X,source.PixelHeight-point.Y,0);
        }
        var old=mesh!.Positions;mesh.Positions=spare;spare=old;model.Geometry=mesh;
        return true;
    }
    private void Configure(BitmapSource source)
    {
        Reset();texture=source;
        var positions=new Point3DCollection(rest.Length);spare=new Point3DCollection(rest.Length);
        var coordinates=new PointCollection(rest.Length);var indices=new Int32Collection(Columns*Rows*6);
        for(var row=0;row<=Rows;row++)for(var column=0;column<=Columns;column++)
        {
            var i=row*(Columns+1)+column;
            rest[i]=new Point(source.PixelWidth*(double)column/Columns,source.PixelHeight*(double)row/Rows);
            positions.Add(new Point3D(rest[i].X,source.PixelHeight-rest[i].Y,0));spare.Add(positions[i]);
            coordinates.Add(new Point((double)column/Columns,(double)row/Rows));
        }
        for(var row=0;row<Rows;row++)for(var column=0;column<Columns;column++)
        {
            var a=row*(Columns+1)+column;var b=a+1;var c=a+Columns+1;var d=c+1;
            indices.Add(a);indices.Add(c);indices.Add(b);indices.Add(b);indices.Add(c);indices.Add(d);
        }
        coordinates.Freeze();indices.Freeze();
        mesh=new MeshGeometry3D{Positions=positions,TextureCoordinates=coordinates,TriangleIndices=indices};
        var brush=new ImageBrush(source){Stretch=Stretch.Fill};brush.Freeze();
        var material=new DiffuseMaterial(brush);material.Freeze();
        model=new GeometryModel3D(mesh,material);
        var scene=new Model3DGroup();scene.Children.Add(new AmbientLight(Colors.White));scene.Children.Add(model);
        Children.Add(new ModelVisual3D{Content=scene});
        camera.Position=new Point3D(source.PixelWidth/2.0,source.PixelHeight/2.0,1000);
        camera.LookDirection=new Vector3D(0,0,-1);camera.UpDirection=new Vector3D(0,1,0);FitCamera();
    }
    private void FitCamera()
    {
        if(texture is null)return;
        var aspect=ActualHeight>0?ActualWidth/ActualHeight:(double)texture.PixelWidth/texture.PixelHeight;
        camera.Width=Math.Max(texture.PixelWidth,texture.PixelHeight*aspect);
    }
    internal void Reset(){if(texture is null)return;Children.Clear();texture=null;mesh=null;model=null;spare=null;previousPose=null;}
}
