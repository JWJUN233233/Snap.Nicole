using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Sentry;
using Snap.Nicole.Core.Diagnostics;
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
    [NotifyPropertyChangedFor(nameof(IsRepositoryUrlEnabled), nameof(IsSetupVisible), nameof(IsOperationsVisible))]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand), nameof(PullRepositoryCommand), nameof(PushRepositoryCommand))]
    public partial bool IsGitAvailable { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepositoryUrlEnabled), nameof(IsSetupVisible), nameof(IsOperationsVisible))]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand), nameof(PullRepositoryCommand), nameof(PushRepositoryCommand))]
    public partial bool IsRepository { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepositoryUrlEnabled), nameof(IsSetupVisible), nameof(IsOperationsVisible))]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand), nameof(PullRepositoryCommand), nameof(PushRepositoryCommand))]
    public partial bool HasRemote { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRepositoryUrlEnabled), nameof(IsProgressVisible))]
    [NotifyCanExecuteChangedFor(nameof(RefreshRepositoryStateCommand), nameof(InitializeRepositoryCommand), nameof(PullRepositoryCommand), nameof(PushRepositoryCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InitializeRepositoryCommand))]
    public partial string RepositoryUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RepositoryPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial StringResourceValue RemoteUrlDisplay { get; set; } = StringResourceValue.FromText(string.Empty);

    [ObservableProperty]
    public partial StringResourceValue CurrentOperationText { get; set; } = StringResourceValue.FromText(string.Empty);

    [ObservableProperty]
    public partial StringResourceValue StatusTitle { get; set; } = StringResourceValue.FromText(string.Empty);

    [ObservableProperty]
    public partial StringResourceValue StatusMessage { get; set; } = StringResourceValue.FromText(string.Empty);

    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; }

    [ObservableProperty]
    public partial InfoBarSeverity InfoBarSeverity { get; set; } = InfoBarSeverity.Informational;

    public bool IsSetupVisible { get => IsGitAvailable && (!IsRepository || !HasRemote); }

    public bool IsOperationsVisible { get => IsGitAvailable && IsRepository && HasRemote; }

    public bool IsProgressVisible { get => IsBusy; }

    public bool IsRepositoryUrlEnabled { get => IsGitAvailable && !IsBusy && (!IsRepository || !HasRemote); }

    private bool CanRefreshRepositoryState { get => !IsBusy; }

    private bool CanInitializeRepository { get => IsRepositoryUrlEnabled && !string.IsNullOrWhiteSpace(RepositoryUrl); }

    private bool CanOperateRepository { get => IsGitAvailable && IsRepository && HasRemote && !IsBusy; }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsFolderOpen, "Open settings folder");

        try
        {
            Directory.CreateDirectory(gitSyncService.RepositoryPath);
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = gitSyncService.RepositoryPath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsFolderOpen);
            Debug.WriteLine(ex);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshRepositoryState))]
    private async Task RefreshRepositoryStateAsync(CancellationToken cancellationToken)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsGitRefresh, "Refresh settings Git repository state");

        IsBusy = true;
        SetStatus(InfoBarSeverity.Informational, SRName.UIXamlPagesSettingsPageGitStatusCheckingTitle, SRName.UIXamlPagesSettingsPageGitStatusCheckingMessage);

        try
        {
            await RefreshRepositoryStateCoreAsync(true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            span.Finish(SpanStatus.Cancelled);
            throw;
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsGitRefresh);
            throw;
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
            SentryOperations.SettingsGitInitialize,
            SRName.UIXamlPagesSettingsPageGitStatusInitializeMessage,
            static (gitSyncService, remoteUrl, token) => gitSyncService.InitializeRepositoryAsync(remoteUrl, token),
            SRName.UIXamlPagesSettingsPageGitStatusInitializeSuccessTitle,
            static result => SRName.UIXamlPagesSettingsPageGitStatusInitializeSuccessMessage,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanOperateRepository))]
    private async Task PullRepositoryAsync(CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync(
            SentryOperations.SettingsGitPull,
            SRName.UIXamlPagesSettingsPageGitStatusPullMessage,
            static (gitSyncService, remoteUrl, token) => gitSyncService.PullAsync(token),
            SRName.UIXamlPagesSettingsPageGitStatusPullSuccessTitle,
            static result => result.DetailKind is SettingsGitOperationDetailKind.RemoteEmpty
                ? SRName.UIXamlPagesSettingsPageGitStatusRemoteEmptyMessage
                : SRName.UIXamlPagesSettingsPageGitStatusPullSuccessMessage,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanOperateRepository))]
    private async Task PushRepositoryAsync(CancellationToken cancellationToken)
    {
        await ExecuteOperationAsync(
            SentryOperations.SettingsGitPush,
            SRName.UIXamlPagesSettingsPageGitStatusPushMessage,
            static (gitSyncService, remoteUrl, token) => gitSyncService.PushAsync(token),
            SRName.UIXamlPagesSettingsPageGitStatusPushSuccessTitle,
            static result => result.DetailKind is SettingsGitOperationDetailKind.NoLocalCommits
                ? SRName.UIXamlPagesSettingsPageGitStatusNoLocalCommitsMessage
                : SRName.UIXamlPagesSettingsPageGitStatusPushSuccessMessage,
            cancellationToken);
    }

    private async Task ExecuteOperationAsync(
        string operationName,
        SRName currentOperationTextName,
        Func<ISettingsGitSyncService, string, CancellationToken, Task<SettingsGitOperationResult>> operation,
        SRName successTitleName,
        Func<SettingsGitOperationResult, StringResourceValue> successMessageFactory,
        CancellationToken cancellationToken)
    {
        string currentOperationText = StringResourceProxy.Default[currentOperationTextName];
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(operationName, currentOperationText);

        IsBusy = true;
        CurrentOperationText = StringResourceValue.FromName(currentOperationTextName);
        SetStatus(InfoBarSeverity.Informational, SRName.UIXamlPagesSettingsPageGitStatusOperationTitle, StringResourceValue.FromName(currentOperationTextName));

        SettingsGitOperationResult result;
        try
        {
            result = await operation(gitSyncService, RepositoryUrl.Trim(), cancellationToken);
            await RefreshRepositoryStateCoreAsync(false, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            span.Finish(SpanStatus.Cancelled);
            SetStatus(InfoBarSeverity.Warning, SRName.UIXamlPagesSettingsPageGitStatusOperationFailedTitle, SRName.UIXamlPagesSettingsPageGitErrorCanceled);
            return;
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, operationName);
            throw;
        }
        finally
        {
            IsBusy = false;
            CurrentOperationText = StringResourceValue.FromText(string.Empty);
        }

        if (result.Succeeded)
        {
            span.SetTag(SentryTags.SettingsGitSucceeded, true);
            span.SetTag(SentryTags.SettingsGitFailureKind, result.FailureKind.ToString());
            SetStatus(InfoBarSeverity.Success, successTitleName, successMessageFactory(result));
            return;
        }

        span.SetTag(SentryTags.SettingsGitSucceeded, false);
        span.SetTag(SentryTags.SettingsGitFailureKind, result.FailureKind.ToString());
        span.Finish(SpanStatus.FailedPrecondition);
        SetStatus(InfoBarSeverity.Error, SRName.UIXamlPagesSettingsPageGitStatusOperationFailedTitle, BuildFailureMessage(result));
    }

    private async Task RefreshRepositoryStateCoreAsync(bool updateStatus, CancellationToken cancellationToken)
    {
        SettingsGitRepositoryState state = await gitSyncService.GetRepositoryStateAsync(cancellationToken);

        RepositoryPath = gitSyncService.RepositoryPath;
        IsGitAvailable = state.IsGitAvailable;
        IsRepository = state.IsRepository;
        HasRemote = !string.IsNullOrWhiteSpace(state.RemoteUrl);
        RemoteUrlDisplay = state.RemoteUrl is null ? SRName.UIXamlPagesSettingsPageLabelUnavailable : state.RemoteUrl;

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
            SetStatus(InfoBarSeverity.Warning, SRName.UIXamlPagesSettingsPageGitStatusUnavailableTitle, BuildFailureMessage(SettingsGitOperationResult.Failure(SettingsGitFailureKind.GitUnavailable, state.Detail)));
            return;
        }

        if (state.IsRepository && HasRemote)
        {
            SetStatus(InfoBarSeverity.Success, SRName.UIXamlPagesSettingsPageGitStatusRepositoryReadyTitle, SRName.UIXamlPagesSettingsPageGitStatusRepositoryReadyMessage);
            return;
        }

        if (state.IsRepository)
        {
            SetStatus(InfoBarSeverity.Warning, SRName.UIXamlPagesSettingsPageGitStatusRemoteMissingTitle, SRName.UIXamlPagesSettingsPageGitStatusRemoteMissingMessage);
            return;
        }

        if (state.FailureKind is SettingsGitFailureKind.Repository or SettingsGitFailureKind.Permission)
        {
            SetStatus(InfoBarSeverity.Error, SRName.UIXamlPagesSettingsPageGitStatusInvalidRepositoryTitle, BuildFailureMessage(SettingsGitOperationResult.Failure(state.FailureKind, state.Detail)));
            return;
        }

        SetStatus(InfoBarSeverity.Informational, SRName.UIXamlPagesSettingsPageGitStatusNotRepositoryTitle, SRName.UIXamlPagesSettingsPageGitStatusNotRepositoryMessage);
    }

    private void SetStatus(InfoBarSeverity severity, StringResourceValue title, StringResourceValue message)
    {
        InfoBarSeverity = severity;
        StatusTitle = title;
        StatusMessage = message;
        IsStatusOpen = true;
    }

    private static StringResourceValue BuildFailureMessage(SettingsGitOperationResult result)
    {
        SRName messageName = result.DetailKind is SettingsGitOperationDetailKind.RepositoryOperationInProgress
            ? SRName.UIXamlPagesSettingsPageGitErrorRepositoryOperationInProgress
            : result.FailureKind switch
            {
                SettingsGitFailureKind.GitUnavailable => SRName.UIXamlPagesSettingsPageGitErrorGitUnavailable,
                SettingsGitFailureKind.Network => SRName.UIXamlPagesSettingsPageGitErrorNetwork,
                SettingsGitFailureKind.Permission => SRName.UIXamlPagesSettingsPageGitErrorPermission,
                SettingsGitFailureKind.Remote => SRName.UIXamlPagesSettingsPageGitErrorRemote,
                SettingsGitFailureKind.Repository => SRName.UIXamlPagesSettingsPageGitErrorRepository,
                SettingsGitFailureKind.Conflict => SRName.UIXamlPagesSettingsPageGitErrorConflict,
                _ => SRName.UIXamlPagesSettingsPageGitErrorCommand,
            };

        if (result.DetailKind is SettingsGitOperationDetailKind.RepositoryOperationInProgress)
        {
            return StringResourceValue.FromName(messageName);
        }

        return StringResourceValue.FromName(messageName, result.Detail);
    }
}
