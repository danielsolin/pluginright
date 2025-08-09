namespace PluginRight.Core.OpenAI;

public interface IOpenAIClient
{
    Task<ChatResponse> CompleteAsync(
        ChatRequest req,
        CancellationToken ct = default
    );
}
