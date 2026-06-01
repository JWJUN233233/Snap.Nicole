using Microsoft.Extensions.AI;
using Sentry;
using Snap.Nicole.Core.Diagnostics;
using Snap.Nicole.Core.IO;
using Snap.Nicole.Services.AI.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Snap.Nicole.Services.AI;

internal sealed class AgentConversationFileStore : IAgentConversationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(AIJsonUtilities.DefaultOptions)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    private readonly string directoryPath;

    public AgentConversationFileStore()
    {
        directoryPath = Path.Combine(WellKnownLocations.Settings, "AgentConversations");
    }

    public IReadOnlyList<AgentConversationData> LoadConversations()
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        List<AgentConversationData> conversations = [];
        foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*.json"))
        {
            try
            {
                using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (JsonSerializer.Deserialize<AgentConversationData>(stream, JsonOptions) is { } conversation)
                {
                    conversations.Add(conversation);
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                using SentryDiagnosticSpan span = SentryDiagnostics.StartSpan("agent.conversation.load", "Load agent conversation");
                span.SetData("agent.conversation.file", filePath);
                SentryDiagnostics.CaptureException(ex, span, "agent.conversation.load");
            }
        }

        return conversations;
    }

    public void SaveConversation(AgentConversationData conversation)
    {
        Directory.CreateDirectory(directoryPath);

        string filePath = GetConversationFilePath(conversation.Id);
        string tempFilePath = $"{filePath}.tmp";

        using (FileStream stream = File.Open(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, conversation, JsonOptions);
        }

        File.Move(tempFilePath, filePath, true);
    }

    public void DeleteConversation(Guid id)
    {
        string filePath = GetConversationFilePath(id);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetConversationFilePath(Guid id)
    {
        return Path.Combine(directoryPath, $"{id:N}.json");
    }
}
