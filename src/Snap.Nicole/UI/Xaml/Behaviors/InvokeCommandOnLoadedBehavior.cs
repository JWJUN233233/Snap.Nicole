using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using System.Windows.Input;

namespace Snap.Nicole.UI.Xaml.Behaviors;

[GeneratedDependencyProperty<ICommand>("Command")]
[GeneratedDependencyProperty<object>("CommandParameter")]
internal sealed partial class InvokeCommandOnLoadedBehavior : BehaviorBase<FrameworkElement>
{
    private bool executed;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject.IsLoaded)
        {
            TryExecuteCommand();
        }
    }

    protected override void OnAssociatedObjectLoaded()
    {
        TryExecuteCommand();
    }

    private void TryExecuteCommand()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        if (executed)
        {
            return;
        }

        if (Command is not null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
            executed = true;
        }
    }
}
