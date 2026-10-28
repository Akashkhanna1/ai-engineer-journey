using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

string endpoint = "AZURE_OPENAI_ENDPOINT";
string key = "AZURE_OPENAI_KEY";
string deployment = "AZURE_OPENAI_CHAT_DEPLOYMENT";

IChatClient client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key)).GetChatClient(deployment).AsIChatClient();
var options = new ChatOptions
{
    Temperature = 0.3f,
    MaxOutputTokens = 500
};

const string SystemPrompt ="You are a concise assistant for .NET developers. Answer in at most 5 sentences.";
var history = new List<ChatMessage> { new(ChatRole.System, SystemPrompt) };
long totalIn = 0, totalOut = 0;

Console.WriteLine("ChatLab started. Commands: 'reset' clears history, 'exit' quits.");

while (true)
{
    Console.Write("\nYou: ");
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    if (input.Equals("reset", StringComparison.OrdinalIgnoreCase))
    {
        history.RemoveRange(1, history.Count - 1);  
        Console.WriteLine("History cleared.");
        continue;
    }

    history.Add(new ChatMessage(ChatRole.User, input));

    try
    {
        var response = await client.GetResponseAsync(history, options);

        Console.WriteLine($"\nAI: {response.Text}");
        history.Add(new ChatMessage(ChatRole.Assistant, response.Text));

        long inTok = response.Usage?.InputTokenCount ?? 0;
        long outTok = response.Usage?.OutputTokenCount ?? 0;
        totalIn += inTok;
        totalOut += outTok;

        Console.WriteLine($"[tokens] in={inTok} out={outTok} | session in={totalIn} out={totalOut} | " +
                          $"messages={history.Count} | finish={response.FinishReason}");

        if (response.FinishReason == ChatFinishReason.Length)
            Console.WriteLine("[warning] Output was cut off by MaxOutputTokens.");
    }
    catch (Exception ex)
    {
        history.RemoveAt(history.Count - 1); 
        Console.WriteLine($"[error] {ex.Message}");
    }
}

