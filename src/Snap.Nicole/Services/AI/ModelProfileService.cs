using Anthropic;
using Anthropic.Core;
using AnthropicModelInfo = Anthropic.Models.Models.ModelInfo;
using AnthropicModelListPage = Anthropic.Models.Models.ModelListPage;
using AnthropicModelListParams = Anthropic.Models.Models.ModelListParams;
using OpenAI;
using OpenAI.Models;
using Snap.Nicole.Core;
using Snap.Nicole.Services.AI.Models;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Snap.Nicole.Services.AI;

internal sealed class ModelProfileService : IModelProfileService
{
    public Task<IReadOnlyList<ModelProfile>> GetModelsAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(providerProfile);

        return providerProfile.ProviderType.Value switch
        {
            ModelProviderType.OpenAIChatCompletion => GetOpenAIModelsAsync(providerProfile, cancellationToken),
            ModelProviderType.OpenAIResponses => GetOpenAIModelsAsync(providerProfile, cancellationToken),
            ModelProviderType.Anthropic => GetAnthropicModelsAsync(providerProfile, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported model provider type: {providerProfile.ProviderType.Value}"),
        };
    }

    private static async Task<IReadOnlyList<ModelProfile>> GetOpenAIModelsAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken)
    {
        ApiKeyCredential apiKeyCredential = new(providerProfile.ApiKey!);
        OpenAIClientOptions options = new()
        {
            Endpoint = providerProfile.Endpoint.ToUri(),
        };

        OpenAIModelClient client = new OpenAIClient(apiKeyCredential, options).GetOpenAIModelClient();
        ClientResult<OpenAIModelCollection> result = await client.GetModelsAsync(cancellationToken);

        IEnumerable<ModelProfile> models = result.Value
            .Where(static model => !string.IsNullOrWhiteSpace(model.Id))
            .Select(static model => ModelProfile.Create(model.Id));
        return [.. models];
    }

    private static async Task<IReadOnlyList<ModelProfile>> GetAnthropicModelsAsync(ModelProviderProfile providerProfile, CancellationToken cancellationToken)
    {
        ClientOptions clientOptions = new()
        {
            ApiKey = providerProfile.ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(providerProfile.Endpoint) ? EnvironmentUrl.Production : providerProfile.Endpoint,
        };

        using (AnthropicClient client = new(clientOptions))
        {
            List<ModelProfile> result = [];
            AnthropicModelListParams @params = new()
            {
                Limit = 1000,
            };

            // TODO: IPage can be adapted to an IAsyncEnumerable
            AnthropicModelListPage page = await client.Models.List(@params, cancellationToken);

            while (true)
            {
                result.AddRange(page.Items
                    .Where(static model => !string.IsNullOrWhiteSpace(model.ID))
                    .Select(CreateAnthropicModelProfile));

                if (!page.HasNext())
                {
                    return result;
                }

                page = await page.Next(cancellationToken);
            }
        }
    }

    private static ModelProfile CreateAnthropicModelProfile(AnthropicModelInfo model)
    {
        string name = string.IsNullOrWhiteSpace(model.DisplayName) ? model.ID : model.DisplayName;
        return new()
        {
            Name = name,
            ModelId = model.ID,
        };
    }
}
