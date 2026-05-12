namespace Snap.Nicole.Services.AI.Models;

[Obsolete]
internal enum ExtendedAIContentKind
{
    Data,
    Error,
    FunctionCall,
    FunctionResult,
    HostedFile,
    HostedVectorStore,
    Text,
    TextReasoning,
    Uri,
    Usage,
    ToolCall,
    ToolResult,
    InputRequest,
    InputResponse,
    ToolApprovalRequest,
    ToolApprovalResponse,
    McpServerToolCall,
    McpServerToolResult,
    ImageGenerationToolCall,
    ImageGenerationToolResult,
    CodeInterpreterToolCall,
    CodeInterpreterToolResult,
    WebSearchToolCall,
    WebSearchToolResult,
}
