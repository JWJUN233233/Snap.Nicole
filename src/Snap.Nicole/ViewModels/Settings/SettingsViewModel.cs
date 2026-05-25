using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Snap.Nicole.Core;
using Snap.Nicole.Resources;
using Snap.Nicole.Services.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.ViewModels.Settings;

internal sealed partial class SettingsViewModel(IServiceProvider serviceProvider) : ObservableObject
{
    private readonly IModelProfileService modelProfileService = serviceProvider.GetRequiredService<IModelProfileService>();

    public AppSettings Settings { get; } = serviceProvider.GetRequiredService<IOptionsProvider<AppSettings>>().CurrentValue;

    public SettingsGitSyncViewModel GitSync { get; } = serviceProvider.GetRequiredService<SettingsGitSyncViewModel>();

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
    public partial string ModelListStatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ModelListStatusMessage { get; set; } = string.Empty;

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

        Settings.ModelProviderProfiles.Remove(selected);
    }

    [RelayCommand]
    private void AddModel()
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            return;
        }

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

        modelProfiles.Remove(selected);
    }

    [RelayCommand]
    private void ClearModels()
    {
        Settings.ModelProviderProfiles.CurrentItem?.ModelProfiles?.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanRefreshModels))]
    private async Task RefreshModelsAsync(CancellationToken cancellationToken)
    {
        if (Settings.ModelProviderProfiles.CurrentItem is not { } providerProfile)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SR.UIXamlPagesSettingsPageModelListStatusFailedTitle, SR.UIXamlPagesSettingsPageModelListStatusNoProviderMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(providerProfile.ModelListDocumentationLink))
        {
            OpenModelListDocumentationLink(providerProfile.ModelListDocumentationLink);
            return;
        }

        if (string.IsNullOrWhiteSpace(providerProfile.ApiKey))
        {
            SetModelListStatus(InfoBarSeverity.Warning, SR.UIXamlPagesSettingsPageModelListStatusFailedTitle, SR.UIXamlPagesSettingsPageModelListStatusApiKeyMissingMessage);
            return;
        }

        IsModelListBusy = true;
        SetModelListStatus(InfoBarSeverity.Informational, SR.UIXamlPagesSettingsPageModelListStatusFetchingTitle, SR.UIXamlPagesSettingsPageModelListStatusFetchingMessage);

        try
        {
            IReadOnlyList<ModelProfile> modelProfiles = await modelProfileService.GetModelsAsync(providerProfile, cancellationToken);
            MergeModelProfiles(providerProfile, modelProfiles);
            SetModelListStatus(InfoBarSeverity.Success, SR.UIXamlPagesSettingsPageModelListStatusSuccessTitle, string.Format(CultureInfo.CurrentCulture, SR.UIXamlPagesSettingsPageModelListStatusSuccessMessage, modelProfiles.Count));
        }
        catch (OperationCanceledException)
        {
            SetModelListStatus(InfoBarSeverity.Warning, SR.UIXamlPagesSettingsPageModelListStatusFailedTitle, SR.UIXamlPagesSettingsPageModelListStatusCanceledMessage);
        }
        catch (Exception ex)
        {
            SetModelListStatus(InfoBarSeverity.Error, SR.UIXamlPagesSettingsPageModelListStatusFailedTitle, ex.Message);
        }
        finally
        {
            IsModelListBusy = false;
        }
    }

    private void OpenModelListDocumentationLink(string link)
    {
        string trimmedLink = link.Trim();
        if (!Uri.TryCreate(trimmedLink, UriKind.Absolute, out Uri? uri) || uri.Scheme is not "http" and not "https")
        {
            SetModelListStatus(InfoBarSeverity.Error, SR.UIXamlPagesSettingsPageModelListStatusFailedTitle, SR.UIXamlPagesSettingsPageModelListStatusInvalidDocumentationLinkMessage);
            return;
        }

        try
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true,
            });

            SetModelListStatus(InfoBarSeverity.Informational, SR.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedTitle, SR.UIXamlPagesSettingsPageModelListStatusDocumentationOpenedMessage);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            SetModelListStatus(InfoBarSeverity.Error, SR.UIXamlPagesSettingsPageModelListStatusFailedTitle, ex.Message);
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

    private void SetModelListStatus(InfoBarSeverity severity, string title, string message)
    {
        ModelListInfoBarSeverity = severity;
        ModelListStatusTitle = title;
        ModelListStatusMessage = message;
        IsModelListStatusOpen = true;
    }
}
