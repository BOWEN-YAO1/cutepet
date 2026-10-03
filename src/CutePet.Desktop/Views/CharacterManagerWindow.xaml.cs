using System.Windows;
using System.Windows.Controls;
namespace CutePet.Desktop;

// Compatibility host for the reusable characters page; the main application opens it in its control center.
public partial class CharacterManagerWindow : Window
{
    private readonly CharactersPage page;
    internal ListBox CharacterList => page.CharacterList;
    internal Button RemoveButton => page.RemoveButton;
    internal Button PreviewButton => page.PreviewButton;
    internal Button UseButton => page.UseButton;
    internal ComboBox PreviewAction => page.PreviewAction;
    internal Image Preview => page.Preview;
    public CharacterManagerWindow(MainWindow host)
    {
        InitializeComponent(); Icon = AppIcon.WindowIcon;
        page = new CharactersPage(host); PageHost.Content = page;
        Closed += (_, _) => page.Dispose();
    }
}
