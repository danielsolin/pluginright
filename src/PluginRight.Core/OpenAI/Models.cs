namespace PluginRight.Core.OpenAI;

public record ChatMessage(string Role, string Content);

public record ChatRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    bool Stream = false
);

public record ChatChoice(string Content);

public record ChatResponse(
    string Content,
    int? PromptTokens = null,
    int? CompletionTokens = null
);
