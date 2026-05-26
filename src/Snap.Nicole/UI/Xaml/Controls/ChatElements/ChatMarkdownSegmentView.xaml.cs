using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Snap.Nicole.UI.Xaml.Controls.ChatElements;

[GeneratedDependencyProperty<string>("Title")]
[GeneratedDependencyProperty<string>("Markdown")]
[GeneratedDependencyProperty<double>("MarkdownOpacity", DefaultValue = 1.0, NotNull = true)]
internal sealed partial class ChatMarkdownSegmentView : UserControl
{
    public ChatMarkdownSegmentView()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void CopyMarkdown()
    {
        string? markdown = Markdown;
        if (string.IsNullOrEmpty(markdown))
        {
            return;
        }

        DataPackage dataPackage = new();
        dataPackage.SetText(markdown);
        Clipboard.SetContent(dataPackage);
    }
}
