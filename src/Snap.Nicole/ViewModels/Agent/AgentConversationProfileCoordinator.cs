using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.AI;
using Snap.Nicole.Core.ComponentModel;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Snap.Nicole.ViewModels.Agent;

internal sealed class AgentConversationProfileCoordinator : IDisposable
{
    private readonly IMessenger messenger;
    private readonly Guid messengerToken;
    private readonly AppSettings settings;
    private readonly NotifyPropertyChangedEventRevoker settingsChangedEventRevoker;
    private INotifyPropertyChanged? modelProviderProfilesSubscription;
    private INotifyPropertyChanged? modelProfilesSubscription;
    private NotifyPropertyChangedEventRevoker? modelProviderProfilesChangedEventRevoker;
    private NotifyPropertyChangedEventRevoker? modelProfilesChangedEventRevoker;
    private bool disposed;
    private bool isApplyingConversationSelection;

    public AgentConversationProfileCoordinator(AppSettings settings, IMessenger messenger, Guid messengerToken)
    {
        this.messenger = messenger;
        this.messengerToken = messengerToken;
        this.settings = settings;

        settingsChangedEventRevoker = NotifyPropertyChangedEvents.AutoRevoke(settings, OnSettingsPropertyChanged);
        UpdateModelProviderProfilesSubscription();
        UpdateModelProfilesSubscription();
    }

    public AppSettings Settings
    {
        get
        {
            return settings;
        }
    }

    public bool HasSelectedModel
    {
        get
        {
            return !string.IsNullOrWhiteSpace(settings.ModelProviderProfiles.CurrentItem?.ModelProfiles.CurrentItem?.ModelId);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        settingsChangedEventRevoker.Dispose();
        ClearPropertyChangedSubscriptions();
    }

    public void InitializeConversationProfile(AgentConversationViewModel conversation)
    {
        ModelProviderProfile? providerProfile = settings.ModelProviderProfiles.CurrentItem;
        ModelProfile? modelProfile = providerProfile?.ModelProfiles.CurrentItem;
        if (providerProfile is not null)
        {
            conversation.ModelProviderProfileId = providerProfile.Id;
            conversation.ProviderType = providerProfile.ProviderType.Value;
        }

        if (modelProfile is not null)
        {
            conversation.ModelProfileId = modelProfile.Id;
            conversation.ModelId = modelProfile.ModelId;
        }
    }

    public ExtendedAgentOptions? CreateRequestOptions()
    {
        ModelProviderProfile? providerProfile = settings.ModelProviderProfiles.CurrentItem;
        if (providerProfile is null)
        {
            return null;
        }

        ModelProfile? modelProfile = providerProfile.ModelProfiles.CurrentItem;
        if (modelProfile is null || string.IsNullOrWhiteSpace(modelProfile.ModelId))
        {
            return null;
        }

        return new()
        {
            ProviderType = providerProfile.ProviderType.Value,
            ModelId = modelProfile.ModelId.Trim(),
            Endpoint = providerProfile.Endpoint,
            ApiKey = providerProfile.ApiKey,
            ReasoningEffort = ReasoningEffort.High,
            ThinkingEnabled = true,
            OmitReasoningEffortWhenThinkingDisabled = true,
        };
    }

    public void UpdateConversationProfile(AgentConversationViewModel conversation, ExtendedAgentOptions requestOptions)
    {
        ModelProviderProfile? providerProfile = settings.ModelProviderProfiles.CurrentItem;
        ModelProfile? modelProfile = providerProfile?.ModelProfiles.CurrentItem;

        conversation.ModelProviderProfileId = providerProfile?.Id;
        conversation.ModelProfileId = modelProfile?.Id;
        conversation.ProviderType = requestOptions.ProviderType;
        conversation.ModelId = requestOptions.ModelId;
    }

    public void RefreshConversationModelId(AgentConversationViewModel conversation)
    {
        if (!conversation.ModelProviderProfileId.HasValue || !conversation.ModelProfileId.HasValue)
        {
            conversation.ModelId = string.Empty;
            return;
        }

        Guid providerProfileId = conversation.ModelProviderProfileId.Value;
        Guid modelProfileId = conversation.ModelProfileId.Value;
        ModelProviderProfile? providerProfile = settings.ModelProviderProfiles.FirstOrDefault(item => item.Id == providerProfileId);
        ModelProfile? modelProfile = providerProfile?.ModelProfiles.FirstOrDefault(item => item.Id == modelProfileId);
        conversation.ModelId = modelProfile?.ModelId ?? string.Empty;
    }

    public void ApplyConversationProfile(AgentConversationViewModel? conversation)
    {
        if (conversation is null)
        {
            return;
        }

        isApplyingConversationSelection = true;
        try
        {
            if (conversation.ModelProviderProfileId.HasValue)
            {
                settings.ModelProviderProfiles.CurrentItemId = conversation.ModelProviderProfileId;
            }

            if (conversation.ModelProfileId.HasValue)
            {
                settings.ModelProviderProfiles.CurrentItem?.ModelProfiles.MoveCurrentTo(conversation.ModelProfileId.Value);
            }
        }
        finally
        {
            isApplyingConversationSelection = false;
        }
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(AppSettings.ModelProviderProfiles))
        {
            UpdateModelProviderProfilesSubscription();
            UpdateModelProfilesSubscription();
        }
    }

    private void OnModelProviderProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(ObservableSettingsCollection<,>.CurrentItem) or nameof(ObservableSettingsCollection<,>.CurrentItemId))
        {
            UpdateModelProfilesSubscription();
            NotifyCurrentProfileChanged();
        }
    }

    private void OnModelProfilesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        if (e.PropertyName is nameof(ObservableSettingsCollection<,>.CurrentItem) or nameof(ObservableSettingsCollection<,>.CurrentItemId))
        {
            NotifyCurrentProfileChanged();
        }
    }

    private void NotifyCurrentProfileChanged()
    {
        if (isApplyingConversationSelection)
        {
            return;
        }

        messenger.Send(new AgentConversationProfileChangedMessage(), messengerToken);
    }

    private void ClearPropertyChangedSubscriptions()
    {
        UpdatePropertyChangedSubscription(ref modelProviderProfilesSubscription, ref modelProviderProfilesChangedEventRevoker, null, OnModelProviderProfilesPropertyChanged);
        UpdatePropertyChangedSubscription(ref modelProfilesSubscription, ref modelProfilesChangedEventRevoker, null, OnModelProfilesPropertyChanged);
    }

    private void UpdateModelProviderProfilesSubscription()
    {
        UpdatePropertyChangedSubscription(ref modelProviderProfilesSubscription, ref modelProviderProfilesChangedEventRevoker, settings.ModelProviderProfiles, OnModelProviderProfilesPropertyChanged);
    }

    private void UpdateModelProfilesSubscription()
    {
        UpdatePropertyChangedSubscription(ref modelProfilesSubscription, ref modelProfilesChangedEventRevoker, settings.ModelProviderProfiles.CurrentItem?.ModelProfiles, OnModelProfilesPropertyChanged);
    }

    private static void UpdatePropertyChangedSubscription(ref INotifyPropertyChanged? subscription, ref NotifyPropertyChangedEventRevoker? eventRevoker, INotifyPropertyChanged? source, PropertyChangedEventHandler handler)
    {
        if (ReferenceEquals(subscription, source))
        {
            return;
        }

        eventRevoker?.Dispose();
        eventRevoker = null;
        subscription = source;

        if (subscription is not null)
        {
            eventRevoker = NotifyPropertyChangedEvents.AutoRevoke(subscription, handler);
        }
    }
}
