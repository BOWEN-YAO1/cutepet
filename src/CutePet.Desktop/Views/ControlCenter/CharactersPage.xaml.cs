using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CutePet.Desktop;

public partial class CharactersPage : UserControl, IDisposable
{
    private readonly MainWindow host;
    private readonly CharacterAnimation animation = new();
    private readonly CloudFlight cloudPreview = new();
    private readonly RotateTransform sidePreviewTilt = new();
    private readonly TranslateTransform sidePreviewShift = new();
    private readonly Stopwatch clock = new();
    private double swingPreviewElapsed;
    private bool previewCloudPose;
    private readonly CharacterFrameRenderer renderer = new();
    private bool rendering;
    private TimeSpan? lastRenderingTime;
    private CharacterPack? Selected => CharacterList.SelectedItem as CharacterPack;
    private string? lastUsedId;
    private sealed record ActionChoice(string Key, string Label);
    public CharactersPage(MainWindow host)
    {
        this.host = host;
        InitializeComponent();
        var sideTransform = new TransformGroup();
        sideTransform.Children.Add(sidePreviewTilt); sideTransform.Children.Add(sidePreviewShift);
        Preview.RenderTransformOrigin = new Point(.5,.5); Preview.RenderTransform = sideTransform;
        // A newly visible preview layer can still have zero size during the button event.
        // Reanchor as soon as layout measures it; do not wait for the next animation tick.
        Preview.SizeChanged += (_, _) => RefreshSidePreview();
        TopPreviewRopes.SizeChanged += (_, _) => RefreshSidePreview();
        host.Characters.Reload();
        host.SetCharacterPackage(host.SelectedCharacter.Id);
        RefreshList(host.SelectedCharacter.Id);
        StatusText.Text = host.Characters.Warning ?? "";
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible&&!rendering)
            {clock.Restart();lastRenderingTime=null;CompositionTarget.Rendering+=OnRendering;rendering=true;}
            else if(!IsVisible){StopRendering();}
        };
        host.DesktopStateChanged += OnHostStateChanged;
        OnHostStateChanged();
    }
    private void OnHostStateChanged()
    {
        CurrentUseText.Text = "当前使用：" + host.SelectedCharacter.Name;
        if (lastUsedId is not null && lastUsedId != host.SelectedCharacter.Id)
            StatusText.Text = "";
        lastUsedId = host.SelectedCharacter.Id;
        var installed = host.Characters.Packs.ToArray();
        if (CharacterList.ItemsSource is not CharacterPack[] shown || !shown.SequenceEqual(installed))
            RefreshList(Selected?.Id ?? host.SelectedCharacter.Id);
    }
    private void OnRendering(object? sender,EventArgs args)
    {
        if(args is RenderingEventArgs frame)
        {if(lastRenderingTime==frame.RenderingTime)return;lastRenderingTime=frame.RenderingTime;}
        var elapsed=clock.Elapsed;clock.Restart();AdvancePreview(elapsed);
    }
    internal void AdvancePreview(TimeSpan elapsed)
    {
        animation.Advance(elapsed);
        swingPreviewElapsed=(swingPreviewElapsed+elapsed.TotalMilliseconds)%3200;
        cloudPreview.Advance(elapsed);
        animation.Flying=previewCloudPose||cloudPreview.Active&&cloudPreview.Opacity==1;
        CloudPreview.Opacity=previewCloudPose?1:cloudPreview.Opacity;
        Preview.Source=renderer.Render(animation);RefreshSidePreview();
    }
    private void StopRendering()
    {
        if(rendering)CompositionTarget.Rendering-=OnRendering;
        rendering=false;lastRenderingTime=null;clock.Reset();renderer.Reset();
    }
    public void Dispose() { host.DesktopStateChanged -= OnHostStateChanged;StopRendering();renderer.Reset(); }
    private void RefreshList(string? selected)
    {
        CharacterList.ItemsSource = host.Characters.Packs.ToArray();
        CharacterList.SelectedItem = host.Characters.Find(selected);
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        animation.Configure(pack);
        renderer.Reset();
        swingPreviewElapsed = 0;
        cloudPreview.Cancel();
        previewCloudPose = false;
        CloudPreview.Opacity = 0;
        CloudPreview.Source = pack.CloudImage;
        CloudPreview.Width = pack.Manifest.Cloud?.DisplayWidth ?? 140;
        CloudPreview.Height = pack.Manifest.Cloud?.DisplayHeight ?? 32;
        Preview.Source = renderer.Render(animation);
        RefreshSidePreview();
        var labels = new[] { ("idle", "待机"), ("blink", "眨眼"), ("greeting", "打招呼"), ("low", "低额度"),
            ("look", "张望"), ("hover", "悬停"), ("happy", "开心"), ("conjure", "召唤王座"), ("sit", "坐下休息"), ("stand", "起身收起"), ("summon-cloud", "召唤小云"), ("cloud-idle", "乘云随风"), ("cloud-blink", "乘云眨眼"),
            ("sit-blink", "坐姿眨眼"), ("sit-greeting", "坐姿挥手"), ("sit-happy", "坐姿微笑"),
            ("edge-idle", "左右贴边"), ("edge-peek", "左右探头微笑"), ("edge-top-idle", "花藤秋千"),
            ("edge-shy", "缩回再探出"), ("edge-sway", "探头轻摇"), ("edge-nod", "探头点头"),
            ("edge-top-peek", "秋千闭眼微笑"), ("edge-top-look", "秋千左右张望"), ("edge-top-smile", "秋千歪头微笑"), ("edge-bottom-idle", "下沿托腮"), ("edge-bottom-peek", "下沿抬头微笑"),
            ("edge-bottom-look", "下沿左右张望"), ("edge-bottom-smile", "下沿歪头微笑") };
        CharacterInfo.Text = pack.Name;
        RightsInfo.Text = $"作者：{(string.IsNullOrWhiteSpace(pack.Manifest.Author) ? "未填写" : pack.Manifest.Author)}\n"
            + (string.IsNullOrWhiteSpace(pack.Manifest.License) ? "未填写素材许可，请确认图片的使用权限。" : pack.Manifest.License);
        RemoveButton.IsEnabled = !pack.BuiltIn;
        PreviewAction.ItemsSource = labels.Where(pair => pack.Actions.ContainsKey(pair.Item1))
            .Select(pair => new ActionChoice(pair.Item1, pair.Item2)).ToArray();
        PreviewAction.SelectedValue = pack.Actions.ContainsKey("greeting") ? "greeting" : "idle";
        PreviewButton.IsEnabled = true;
        clock.Restart();
    }
    private void OnUse(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        host.SetCharacterPackage(pack.Id);
        StatusText.Text = "";
    }
    private void OnPreview(object sender, RoutedEventArgs e)
    {
        renderer.Reset();
        swingPreviewElapsed = 0;
        cloudPreview.Cancel();
        previewCloudPose = PreviewAction.SelectedValue is "cloud-idle" or "cloud-blink";
        if (PreviewAction.SelectedValue is string action)
        { animation.Preview(action); if (action == "summon-cloud") cloudPreview.Start(Selected!.Actions[action].Duration); }
        CloudPreview.Opacity = previewCloudPose ? 1 : 0;
        Preview.Source = renderer.Render(animation);
        RefreshSidePreview();
        // Loading/selecting a role may have blocked the UI. Start the new clip's
        // clock after its initial drawing, not at the previous rendering event.
        clock.Restart();
    }
    private void RefreshSidePreview()
    {
        BottomPreviewEdge.Visibility = Visibility.Collapsed;
        TopPreviewRopes.Visibility = Visibility.Collapsed;
        sidePreviewTilt.CenterY = 0;
        if (Selected is { Manifest.TopSwing: { } swing } top && EdgeActions.BaseOf(animation.Action) == "edge-top-idle"
            && top.UsesFrameRegions(animation.Action))
        {
            SidePreviewEdge.Visibility = Visibility.Collapsed;
            TopPreviewRopes.Visibility = Visibility.Visible;
            Preview.Clip = null;
            var frame = animation.SpriteFrame;
            var height = Math.Min(Preview.ActualHeight,Preview.ActualWidth * frame.Image.PixelHeight / frame.Image.PixelWidth);
            var width = Math.Min(Preview.ActualWidth,Preview.ActualHeight * frame.Image.PixelWidth / frame.Image.PixelHeight);
            var anchor = frame.EdgeAnchorY ?? top.Manifest.EdgeTopAnchorY;
            var responding = EdgeActions.TopResponses.Contains(animation.Action);
            var amplitude = 3 + (responding ? Math.Pow(Math.Sin(Math.PI * animation.ActionProgress),2) : 0);
            sidePreviewTilt.CenterY = height * (anchor - .5);
            sidePreviewTilt.Angle = amplitude * Math.Sin(2*Math.PI*swingPreviewElapsed/3200);
            sidePreviewShift.X = 0;
            sidePreviewShift.Y = -(Preview.ActualHeight-height)/2 - height*anchor;
            foreach(var (rope,sign) in new[] {(TopPreviewLeft,-1),(TopPreviewRight,1)})
            {
                var span=sign*width*swing.SeatHalfWidth;
                var point=Preview.TranslatePoint(new Point(Preview.ActualWidth/2+span,(Preview.ActualHeight-height)/2
                    + height*(frame.SwingSeatAnchorY ?? swing.SeatAnchorY)),TopPreviewRopes);
                rope.X1=TopPreviewRopes.ActualWidth/2+span;rope.Y1=0;
                rope.X2=point.X;rope.Y2=point.Y;
            }
            return;
        }
        if (Selected is { } bottom && EdgeActions.BaseOf(animation.Action) == "edge-bottom-idle"
            && bottom.UsesFrameRegions(animation.Action))
        {
            SidePreviewEdge.Visibility = Visibility.Collapsed;
            BottomPreviewEdge.Visibility = Visibility.Visible;
            var frame = animation.SpriteFrame;
            var height = Math.Min(Preview.ActualHeight,Preview.ActualWidth * frame.Image.PixelHeight / frame.Image.PixelWidth);
            var anchor = (Preview.ActualHeight - height) / 2 + height * (frame.EdgeAnchorY ?? bottom.Manifest.EdgeBottomAnchorY);
            Preview.Clip = new RectangleGeometry(new Rect(0,0,Preview.ActualWidth,anchor));
            sidePreviewTilt.Angle = 0; sidePreviewShift.X = 0;
            sidePreviewShift.Y = Preview.ActualHeight - anchor;
            return;
        }
        if (Selected is { } pack && EdgeActions.BaseOf(animation.Action) == "edge-idle"
            && pack.UsesFrameRegions(animation.Action))
        {
            SidePreviewEdge.Visibility = Visibility.Visible;
            var frame = animation.SpriteFrame;
            var width = Math.Min(Preview.ActualWidth, Preview.ActualHeight * frame.Image.PixelWidth / frame.Image.PixelHeight);
            var height = Math.Min(Preview.ActualHeight, Preview.ActualWidth * frame.Image.PixelHeight / frame.Image.PixelWidth);
            var anchor = (Preview.ActualWidth - width) / 2 + width * (frame.EdgeAnchorX ?? pack.Manifest.EdgeAnchorX);
            Preview.Clip = new RectangleGeometry(new Rect(anchor, 0, Math.Max(0,Preview.ActualWidth - anchor), Preview.ActualHeight));
            sidePreviewTilt.Angle = 0;
            sidePreviewShift.X = Preview.ActualWidth / 2 - anchor;
            sidePreviewShift.Y = height * ((pack.Actions["edge-idle"].Frames[0].EdgeAnchorY ?? .58) - (frame.EdgeAnchorY ?? .58));
            return;
        }
        Preview.Clip = null;
        SidePreviewEdge.Visibility = Visibility.Collapsed;
        var pose = SideEdgeMotion.Responses.Contains(animation.Action) ? SideEdgeMotion.Sample(animation.Action, animation.ActionProgress) : default;
        sidePreviewTilt.Angle = pose.Angle; sidePreviewShift.X = pose.Peek; sidePreviewShift.Y = pose.Lift;
    }
    private void OnImport(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFileDialog { Title = "导入自定义角色",
            Filter = "角色图片与角色包|*.png;*.zip|透明 PNG 图片|*.png|CutePet 角色包|*.zip", CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) != true) return;
        TryAction(() =>
        {
            var imported = host.ImportCharacter(picker.FileName);
            RefreshList(imported.Id);
            StatusText.Text = $"已导入并使用「{imported.Name}」。";
        });
    }
    private void OnExport(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        var picker = new Microsoft.Win32.SaveFileDialog { Title = "导出角色包", FileName = pack.Id + ".cutepet.zip",
            Filter = "CutePet 角色包|*.zip", DefaultExt = ".zip", AddExtension = true, OverwritePrompt = true };
        if (picker.ShowDialog(Window.GetWindow(this)) != true) return;
        TryAction(() => { host.Characters.Export(pack, picker.FileName); StatusText.Text = "角色包已导出。"; });
    }
    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        TryAction(() =>
        {
            host.RemoveCharacter(pack);
            RefreshList(host.SelectedCharacter.Id);
            StatusText.Text = "角色已移除。";
        });
    }
    private void TryAction(Action action)
    {
        try { action(); }
        catch (Exception ex) when (CharacterLibrary.IsPackageError(ex))
        {
            StatusText.Text = ex is InvalidDataException ? ex.Message : "操作未完成，请检查文件格式和目录权限。";
        }
    }
}
