using Microsoft.Extensions.AI;
using Snap.Nicole.Services.AI.Models;
using Snap.Nicole.Services.Settings;
using System.Linq;

namespace Snap.Nicole.ViewModels.Agent;

internal sealed class AgentConversationProfileCoordinator(AppSettings settings)
{
    private readonly AppSettings settings = settings;

    public static ExtendedAgentOptions? CreateRequestOptions(AgentConversationViewModel conversation)
    {
        if (conversation.ModelProviderProfile is not { } providerProfile)
        {
            return null;
        }

        if (conversation.ModelProfile is not { } modelProfile || string.IsNullOrWhiteSpace(modelProfile.ModelId))
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

    public void ResolveConversationProfile(AgentConversationViewModel conversation, Guid? providerProfileId, Guid? modelProfileId)
    {
        ModelProviderProfile? providerProfile = FindModelProviderProfile(settings.ModelProviderProfiles, providerProfileId);
        ModelProfile? modelProfile = FindModelProfile(providerProfile?.ModelProfiles, modelProfileId);

        conversation.ModelProviderProfile = providerProfile;
        conversation.ModelProfile = modelProfile;
    }

    private static ModelProviderProfile? FindModelProviderProfile(ObservableSettingsCollection<ModelProviderProfile, Guid> collection, Guid? providerProfileId)
    {
        if (!providerProfileId.HasValue)
        {
            return collection.CurrentItem;
        }

        return collection.FirstOrDefault(profile => profile.Id == providerProfileId.Value) ?? collection.CurrentItem;
    }

    private static ModelProfile? FindModelProfile(ObservableSettingsCollection<ModelProfile, Guid>? collection, Guid? modelProfileId)
    {
        if (collection is null)
        {
            return null;
        }

        if (!modelProfileId.HasValue)
        {
            return collection.CurrentItem;
        }

        return collection.FirstOrDefault(profile => profile.Id == modelProfileId.Value) ?? collection.CurrentItem;
    }
}
