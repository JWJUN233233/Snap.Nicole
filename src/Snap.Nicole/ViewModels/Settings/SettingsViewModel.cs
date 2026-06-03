using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Sentry;
using Snap.Nicole.Core;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels.Settings;

internal sealed partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly IModelProfileService modelProfileService;
    private bool disposed;

    public SettingsViewModel(IServiceProvider serviceProvider)
    {
        modelProfileService = serviceProvider.GetRequiredService<IModelProfileService>();
        Settings = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;
        GitSync = serviceProvider.GetRequiredService<SettingsGitSyncViewModel>();

        Settings.ModelProviderProfiles.CollectionChanged += OnModelProviderProfilesCollectionChanged;
        ModelProviderProfileEmptyStateText = CreateModelProviderProfileEmptyStateText();
    }

    public AppSettings Settings { get; }

    public SettingsGitSyncViewModel GitSync { get; }

    // TODO: Potentially cache this list
    public IReadOnlyList<SettingsItem<string>> Languages { get; } = [.. StringResourceProxy.SupportedCultures.Select(name => new SettingsItem<string>(CultureInfo.GetCultureInfo(name).NativeName, name))];

    public IReadOnlyList<SettingsItem<EnumBox<ModelProviderType>>> ModelProviderTypes { get; } =
    [
        new("OpenAI Chat Completions | ~/chat/completions", EnumBox.Of(ModelProviderType.OpenAIChatCompletion)),
        new("OpenAI Responses | ~/responses", EnumBox.Of(ModelProviderType.OpenAIResponses)),
        new("Anthropic Messages | ~/v1/messages", EnumBox.Of(ModelProviderType.Anthropic)),
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshModelsCommand))]
    public partial bool IsModelListBusy { get; set; }

    [ObservableProperty]
    public partial StringResourceValue ModelListStatusTitle { get; set; } = StringResourceValue.FromText(string.Empty);

    [ObservableProperty]
    public partial StringResourceValue ModelListStatusMessage { get; set; } = StringResourceValue.FromText(string.Empty);

    [ObservableProperty]
    public partial StringResourceValue ModelProviderProfileEmptyStateText { get; set; } = StringResourceValue.FromText(string.Empty);

    [ObservableProperty]
    public partial bool IsModelListStatusOpen { get; set; }

    [ObservableProperty]
    public partial InfoBarSeverity ModelListInfoBarSeverity { get; set; } = InfoBarSeverity.Informational;

    private bool CanRefreshModels
    {
        get => !IsModelListBusy;
    }

    [RelayCommand]
    private void AddProfile()
    {
        SentryDiagnostics.AddBreadcrumb("Add provider profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);

        ModelProviderProfile providerProfile = new();
        ModelProfile modelProfile = new();

        providerProfile.ModelProfiles.Add(modelProfile);
        providerProfile.ModelProfiles.CurrentItem = modelProfile;
        Settings.ModelProviderProfiles.Add(providerProfile);
        Settings.ModelProviderProfiles.CurrentItem = providerProfile;
    }

    [RelayCommand]
    private void DeleteProfile()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Delete provider profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        Settings.ModelProviderProfiles.Remove(selected);
    }

    [RelayCommand]
    private void AddModel()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Add model profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        ModelProfile modelProfile = new();

        providerProfile.ModelProfiles.Add(modelProfile);
        providerProfile.ModelProfiles.CurrentItem = modelProfile;
    }

    [RelayCommand]
    private void DeleteModel()
    {
        if (Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles is not { } modelProfiles)
        {
            return;
        }

        if (modelProfiles.CurrentItem is not { } selected)
        {
            return;
        }

        SentryDiagnostics.AddBreadcrumb("Delete model profile", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        modelProfiles.Remove(selected);
    }

    [RelayCommand]
    private void ClearModels()
    {
        SentryDiagnostics.AddBreadcrumb("Clear model profiles", SentryBreadcrumbCategories.SettingsModelProfiles, SentryBreadcrumbTypes.UI);
        Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles?.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanRefreshModels))]
    private async Task RefreshModelsAsync(CancellationToken cancellationToken)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsModelProfilesRefresh, "Refresh model list");

        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SRName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SRName.UIXamlPagesSettingsPageModelListStatusNoProviderMessage);
            span.Finish(SpanStatus.FailedPrecondition);
            return;
        }

        span.SetTag(SentryTags.AIProvider, providerProfile.ProviderType.Value.ToString());

        if (!string.IsNullOrWhiteSpace(providerProfile.ModelListDocumentationLink))
        {
            OpenModelListDocumentationLink(providerProfile.ModelListDocumentationLink);
            return;
        }

        if (string.IsNullOrWhiteSpace(providerProfile.ApiKey))
        {
            SetModelListStatus(InfoBarSeverity.Warning, SRName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SRName.UIXamlPagesSettingsPageModelListStatusApiKeyMissingMessage);
            span.Finish(SpanStatus.FailedPrecondition);
            return;
        }

        IsModelListBusy = true;
        SetModelListStatus(InfoBarSeverity.Informational, SRName.UIXamlPagesSettingsPageModelListStatusFetchingTitle, SRName.UIXamlPagesSettingsPageModelListStatusFetchingMessage);

        try
        {
            IReadOnlyList<ModelProfile> modelProfiles = await modelProfileService.GetModelsAsync(providerProfile, cancellationToken);
            MergeModelProfiles(providerProfile, modelProfiles);
            SetModelListStatus(InfoBarSeverity.Success, SRName.UIXamlPagesSettingsPageModelListStatusSuccessTitle, StringResourceValue.FromName(SRName.UIXamlPagesSettingsPageModelListStatusSuccessMessage, modelProfiles.Count));
            span.SetData(SentryData.AIModelCount, modelProfiles.Count);
        }
        catch (OperationCanceledException)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SRName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SRName.UIXamlPagesSettingsPageModelListStatusCanceledMessage);
            span.Finish(SpanStatus.Cancelled);
        }
        catch (Exception ex)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsModelProfilesRefresh);
            SetModelListStatus(InfoBarSeverity.Error, SRName.UIXamlPagesSettingsPageModelListStatusFailedTitle, StringResourceValue.FromText(ex.Message));
        }
        finally
        {
            IsModelListBusy = false;
        }
    }

    private void OpenModelListDocumentationLink(string link)
    {
        using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan(SentryOperations.SettingsModelProfilesOpenDocs, "Open model list documentation");

        string trimmedLink = link.Trim();
        if (!Uri.TryCreate(trimmedLink, UriKind.Absolute, out Uri? uri) || uri.Scheme is not "http" and not "https")
        {
            SetModelListStatus(InfoBarSeverity.Error, SRName.UIXamlPagesSettingsPageModelListStatusFailedTitle, SRName.UIXamlPagesSettingsPageModelListStatusInvalidDocumentationLinkMessage);
            span.Finish(SpanStatus.InvalidArgument);
            return;
        }

        try
        {
            span.SetTag(SentryTags.UrlScheme, uri.Scheme);
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true,
            });

            SetModelListStatus(InfoBarSeverity.Informational, SRName.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedTitle, SRName.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedMessage);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            SentryDiagnostics.CaptureException(ex, span, SentryOperations.SettingsModelProfilesOpenDocs);
            SetModelListStatus(InfoBarSeverity.Error, SRName.UIXamlPagesSettingsPageModelListStatusFailedTitle, StringResourceValue.FromText(ex.Message));
        }
    }

    private static void MergeModelProfiles(ModelProviderProfile providerProfile, IReadOnlyList<ModelProfile> source)
    {
        Dictionary<string, ModelProfile> existingMap = new(StringComparer.Ordinal);
        foreach (ModelProfile modelProfile in providerProfile.ModelProfiles)
        {
            if (!string.IsNullOrWhiteSpace(modelProfile.ModelId))
            {
                existingMap.TryAdd(modelProfile.ModelId, modelProfile);
            }
        }

        HashSet<string> visitedModelIds = new(StringComparer.Ordinal);
        bool shouldSelectFirstFetched = providerProfile.ModelProfiles.CurrentItem is null || string.IsNullOrWhiteSpace(providerProfile.ModelProfiles.CurrentItem.ModelId);
        ModelProfile? firstFetched = null;
        int targetIndex = 0;

        foreach (ModelProfile sourceModelProfile in source)
        {
            if (string.IsNullOrWhiteSpace(sourceModelProfile.ModelId) || !visitedModelIds.Add(sourceModelProfile.ModelId))
            {
                continue;
            }

            if (!existingMap.TryGetValue(sourceModelProfile.ModelId, out ModelProfile? targetModelProfile))
            {
                targetModelProfile = new();
                providerProfile.ModelProfiles.Insert(targetIndex, targetModelProfile);
            }
            else
            {
                int currentIndex = providerProfile.ModelProfiles.IndexOf(targetModelProfile);
                if (currentIndex != targetIndex)
                {
                    providerProfile.ModelProfiles.Move(currentIndex, targetIndex);
                }
            }

            targetModelProfile.Name = sourceModelProfile.Name;
            targetModelProfile.ModelId = sourceModelProfile.ModelId;
            firstFetched ??= targetModelProfile;
            targetIndex++;
        }

        if (shouldSelectFirstFetched && firstFetched is not null)
        {
            providerProfile.ModelProfiles.CurrentItem = firstFetched;
        }
    }

    private void SetModelListStatus(InfoBarSeverity severity, SRName titleName, SRName messageName)
    {
        SetModelListStatus(severity, titleName, StringResourceValue.FromName(messageName));
    }

    private void SetModelListStatus(InfoBarSeverity severity, SRName titleName, StringResourceValue message)
    {
        ModelListInfoBarSeverity = severity;
        ModelListStatusTitle = StringResourceValue.FromName(titleName);
        ModelListStatusMessage = message;
        IsModelListStatusOpen = true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Settings.ModelProviderProfiles.CollectionChanged -= OnModelProviderProfilesCollectionChanged;
    }

    private void OnModelProviderProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ModelProviderProfileEmptyStateText = CreateModelProviderProfileEmptyStateText();
    }

    private StringResourceValue CreateModelProviderProfileEmptyStateText()
    {
        return StringResourceValue.FromName(Settings.ModelProviderProfiles.Count > 0
            ? SRName.UIXamlPagesSettingsPageDescriptionSelectModelProviderProfileToEdit
            : SRName.UIXamlPagesSettingsPageDescriptionAddModelProviderProfileToEdit);
    }
}
