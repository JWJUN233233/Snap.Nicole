using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.Git;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels.Settings;

internal sealed partial class SettingsGitSyncViewModel(ISettingsGitSyncService gitSyncService) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PullRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushRepositoryCommand))]
    public partial bool IsGitAvailable { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PullRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushRepositoryCommand))]
    public partial bool IsRepository { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PullRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushRepositoryCommand))]
    public partial bool HasRemote { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshRepositoryStateCommand))]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PullRepositoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushRepositoryCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand))]
    public partial string RepositoryUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RepositoryPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RemoteUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentOperationText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; }

    [ObservableProperty]
    public partial InfoBarSeverity InfoBarSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial Visibility SetupVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility OperationsVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial Visibility ProgressVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial bool IsRepositoryUrlEnabled { get; set; }

    private bool CanRefreshRepositoryState
    {
        get => !IsBusy;
    }

    private bool CanInitializeRepository
    {
        get => IsGitAvailable
            && !IsBusy
            && (!IsRepository || !HasRemote)
            && !string.IsNullOrWhiteSpace(RepositoryUrl);
    }

    private bool CanOperateRepository
    {
        get => IsGitAvailable && IsRepository && HasRemote && !IsBusy;
    }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        try
        {
            Directory.CreateDirectory(gitSyncService.RepositoryPath);
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = gitSyncService.RepositoryPath,
                UseShellExecute = true,
            });

            SetStatus(InfoBarSeverity.Success, SR.UIXamlPagesSettingsPageOpenSettingsFolderSuccessTitle, SR.UIXamlPagesSettingsPageOpenSettingsFolderSuccessMessage);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
        {
            SetStatus(InfoBarSeverity.Error, SR.UIXamlPagesSettingsPageOpenSettingsFolderFailedTitle, $"{SR.UIXamlPagesSettingsPageOpenSettingsFolderFailedMessage}{Environment.NewLine}{ex.Message}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshRepositoryState))]
    private async Task RefreshRepositoryStateAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        SetStatus(InfoBarSeverity.Informational, SR.UIXamlPagesSettingsPageGitStatusCheckingTitle, SR.UIXamlPagesSettingsPageGitStatusCheckingMessage);

        try
        {
            await RefreshRepositoryStateCoreAsync(true, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanInitializeRepository))]
    private async Task InitializeRepositoryAsync(CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync(
            SR.UIXamlPagesSettingsPageGitStatusInitializeMessage,
            gitSyncService.InitializeRepositoryAsync,
            SR.UIXamlPagesSettingsPageGitStatusInitializeSuccessTitle,
            static result => SR.UIXamlPagesSettingsPageGitStatusInitializeSuccessMessage,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanOperateRepository))]
    private async Task PullRepositoryAsync(CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync(
            SR.UIXamlPagesSettingsPageGitStatusPullMessage,
            gitSyncService.PullAsync,
            SR.UIXamlPagesSettingsPageGitStatusPullSuccessTitle,
            GetPullSuccessMessage,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanOperateRepository))]
    private async Task PushRepositoryAsync(CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync(
            SR.UIXamlPagesSettingsPageGitStatusPushMessage,
            gitSyncService.PushAsync,
            SR.UIXamlPagesSettingsPageGitStatusPushSuccessTitle,
            GetPushSuccessMessage,
            cancellationToken);
    }

    partial void OnIsGitAvailableChanged(bool value)
    {
        UpdateUiState();
    }

    partial void OnIsRepositoryChanged(bool value)
    {
        UpdateUiState();
    }

    partial void OnHasRemoteChanged(bool value)
    {
        UpdateUiState();
    }

    partial void OnIsBusyChanged(bool value)
    {
        UpdateUiState();
    }

    private async Task ExecuteOperationAsync(
        string currentOperationText,
        Func<string, CancellationToken, Task<SettingsGitOperationResult>> operation,
        string successTitle,
        Func<SettingsGitOperationResult, string> successMessageFactory,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        CurrentOperationText = currentOperationText;
        SetStatus(InfoBarSeverity.Informational, SR.UIXamlPagesSettingsPageGitStatusOperationTitle, currentOperationText);

        SettingsGitOperationResult result;
        try
        {
            result = await operation(RepositoryUrl.Trim(), cancellationToken);
            await RefreshRepositoryStateCoreAsync(false, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            SetStatus(InfoBarSeverity.Warning, SR.UIXamlPagesSettingsPageGitStatusOperationFailedTitle, SR.UIXamlPagesSettingsPageGitErrorCanceled);
            return;
        }
        finally
        {
            IsBusy = false;
            CurrentOperationText = string.Empty;
        }

        if (result.Succeeded)
        {
            SetStatus(InfoBarSeverity.Success, successTitle, successMessageFactory(result));
            return;
        }

        SetStatus(InfoBarSeverity.Error, SR.UIXamlPagesSettingsPageGitStatusOperationFailedTitle, BuildFailureMessage(result));
    }

    private async Task ExecuteOperationAsync(
        string currentOperationText,
        Func<CancellationToken, Task<SettingsGitOperationResult>> operation,
        string successTitle,
        Func<SettingsGitOperationResult, string> successMessageFactory,
        CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync(
            currentOperationText,
            (_, token) => operation(token),
            successTitle,
            successMessageFactory,
            cancellationToken);
    }

    private async Task RefreshRepositoryStateCoreAsync(bool updateStatus, CancellationToken cancellationToken)
    {
        SettingsGitRepositoryState state = await gitSyncService.GetRepositoryStateAsync(cancellationToken);

        RepositoryPath = gitSyncService.RepositoryPath;
        IsGitAvailable = state.IsGitAvailable;
        IsRepository = state.IsRepository;
        HasRemote = !string.IsNullOrWhiteSpace(state.RemoteUrl);
        RemoteUrl = state.RemoteUrl ?? SR.UIXamlPagesSettingsPageLabelUnavailable;

        if (!string.IsNullOrWhiteSpace(state.RemoteUrl))
        {
            RepositoryUrl = state.RemoteUrl;
        }

        if (!updateStatus)
        {
            return;
        }

        if (!state.IsGitAvailable)
        {
            SetStatus(InfoBarSeverity.Warning, SR.UIXamlPagesSettingsPageGitStatusUnavailableTitle, BuildFailureMessage(SettingsGitOperationResult.Failure(SettingsGitFailureKind.GitUnavailable, state.Detail)));
            return;
        }

        if (state.IsRepository && HasRemote)
        {
            SetStatus(InfoBarSeverity.Success, SR.UIXamlPagesSettingsPageGitStatusRepositoryReadyTitle, SR.UIXamlPagesSettingsPageGitStatusRepositoryReadyMessage);
            return;
        }

        if (state.IsRepository)
        {
            SetStatus(InfoBarSeverity.Warning, SR.UIXamlPagesSettingsPageGitStatusRemoteMissingTitle, SR.UIXamlPagesSettingsPageGitStatusRemoteMissingMessage);
            return;
        }

        if (state.FailureKind is SettingsGitFailureKind.Repository or SettingsGitFailureKind.Permission)
        {
            SetStatus(InfoBarSeverity.Error, SR.UIXamlPagesSettingsPageGitStatusInvalidRepositoryTitle, BuildFailureMessage(SettingsGitOperationResult.Failure(state.FailureKind, state.Detail)));
            return;
        }

        SetStatus(InfoBarSeverity.Informational, SR.UIXamlPagesSettingsPageGitStatusNotRepositoryTitle, SR.UIXamlPagesSettingsPageGitStatusNotRepositoryMessage);
    }

    private void UpdateUiState()
    {
        bool canConfigureRepository = IsGitAvailable && !IsBusy && (!IsRepository || !HasRemote);
        bool canOperateRepository = IsGitAvailable && IsRepository && HasRemote;

        IsRepositoryUrlEnabled = canConfigureRepository;
        SetupVisibility = IsGitAvailable && (!IsRepository || !HasRemote)
            ? Visibility.Visible
            : Visibility.Collapsed;
        OperationsVisibility = canOperateRepository
            ? Visibility.Visible
            : Visibility.Collapsed;
        ProgressVisibility = IsBusy
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SetStatus(InfoBarSeverity severity, string title, string message)
    {
        InfoBarSeverity = severity;
        StatusTitle = title;
        StatusMessage = message;
        IsStatusOpen = true;
    }

    private static string GetPullSuccessMessage(SettingsGitOperationResult result)
    {
        return result.Detail is "RemoteEmpty"
            ? SR.UIXamlPagesSettingsPageGitStatusRemoteEmptyMessage
            : SR.UIXamlPagesSettingsPageGitStatusPullSuccessMessage;
    }

    private static string GetPushSuccessMessage(SettingsGitOperationResult result)
    {
        return result.Detail is "NoLocalCommits"
            ? SR.UIXamlPagesSettingsPageGitStatusNoLocalCommitsMessage
            : SR.UIXamlPagesSettingsPageGitStatusPushSuccessMessage;
    }

    private static string BuildFailureMessage(SettingsGitOperationResult result)
    {
        string message = result.Detail is "RepositoryOperationInProgress"
            ? SR.UIXamlPagesSettingsPageGitErrorRepositoryOperationInProgress
            : result.FailureKind switch
            {
                SettingsGitFailureKind.GitUnavailable => SR.UIXamlPagesSettingsPageGitErrorGitUnavailable,
                SettingsGitFailureKind.Network => SR.UIXamlPagesSettingsPageGitErrorNetwork,
                SettingsGitFailureKind.Permission => SR.UIXamlPagesSettingsPageGitErrorPermission,
                SettingsGitFailureKind.Remote => SR.UIXamlPagesSettingsPageGitErrorRemote,
                SettingsGitFailureKind.Repository => SR.UIXamlPagesSettingsPageGitErrorRepository,
                SettingsGitFailureKind.Conflict => SR.UIXamlPagesSettingsPageGitErrorConflict,
                _ => SR.UIXamlPagesSettingsPageGitErrorCommand,
            };

        if (string.IsNullOrWhiteSpace(result.Detail) || result.Detail is "RepositoryOperationInProgress")
        {
            return message;
        }

        return $"{message}{Environment.NewLine}{result.Detail}";
    }
}
