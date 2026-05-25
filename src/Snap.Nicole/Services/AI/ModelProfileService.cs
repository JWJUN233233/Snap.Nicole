using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Models;
using OpenAI;
using OpenAI.Models;
using Snap.Nicole.Core;
using Snap.Nicole.Services.AI.Models;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            ModelProviderType.OpenAIChatCompletion or ModelProviderType.OpenAIResponses => GetOpenAIModelsAsync(providerProfile, cancellationToken),
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
            .Where(static model => string.IsNotNullOrWhiteSpace(model.Id))
            .Where(static model =>
            {
                IDictionary<string, BinaryData> additionalRawData = GetSerializedAdditionalRawData(model);
                if (additionalRawData.TryGetValue("status", out BinaryData? statusData))
                {
                    string? status = statusData.ToObjectFromJson<string>();
                    if (status is "Shutdown")
                    {
                        return false;
                    }
                }

                return true;
            })
            .Select(ModelProfile.Create);
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
            ModelListParams @params = new()
            {
                Limit = 1000,
            };

            ModelListPage page = await client.Models.List(@params, cancellationToken);
            await foreach(ModelInfo model in page.Paginate(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(model.ID))
                {
                    continue;
                }

                result.Add(ModelProfile.Create(model));
            }

            return result;
        }
    }

    // internal IDictionary<string, BinaryData> SerializedAdditionalRawData
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SerializedAdditionalRawData")]
    private static extern IDictionary<string, BinaryData> GetSerializedAdditionalRawData(OpenAIModel model);
}
