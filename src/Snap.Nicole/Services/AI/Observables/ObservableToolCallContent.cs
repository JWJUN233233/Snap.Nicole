using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace Snap.Nicole.Services.AI.Observables;

[JsonPolymorphic]
[JsonDerivedType(typeof(ObservableCodeInterpreterToolCallContent), "code_interpreter_tool_call")]
[JsonDerivedType(typeof(ObservableFunctionCallContent), "function_call")]
[JsonDerivedType(typeof(ObservableImageGenerationToolCallContent), "image_generation_tool_call")]
[JsonDerivedType(typeof(ObservableMcpServerToolCallContent), "mcp_server_tool_call")]
[JsonDerivedType(typeof(ObservableWebSearchToolCallContent), "web_search_tool_call")]
internal partial class ObservableToolCallContent : ObservableAIContent
{
    [ObservableProperty]
    public partial string CallId { get; set; }
}
