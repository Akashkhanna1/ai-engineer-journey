using System.Globalization;
using System.Text;
using Microsoft.ML.Tokenizers;

Console.OutputEncoding = Encoding.UTF8;   

const int ContextWindow = 128_000;
var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");

Console.WriteLine("=== TokenLab ===");
Console.WriteLine("Enter prices from the Azure OpenAI pricing page for your model.");
decimal inputPrice = ReadDecimal("Input  price per 1M tokens (USD): ");
decimal outputPrice = ReadDecimal("Output price per 1M tokens (USD): ");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1) Analyze text and estimate cost");
    Console.WriteLine("2) Temperature demo (softmax)");
    Console.WriteLine("3) Chat history cost growth");
    Console.WriteLine("0) Exit");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": AnalyzeText(); break;
        case "2": TemperatureDemo(); break;
        case "3": ChatGrowthDemo(); break;
        case "0": return;
        default: Console.WriteLine("Invalid choice."); break;
    }
}

// ---------- 1) Token + cost analyzer 
void AnalyzeText()
{
    Console.WriteLine("Paste text (single line), then Enter:");
    var text = Console.ReadLine() ?? "";

    int inputTokens = tokenizer.CountTokens(text);
    int words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    Console.Write($"Expected output tokens (Enter = {inputTokens}): ");
    var raw = Console.ReadLine();
    int outputTokens = int.TryParse(raw, out var o) ? o : inputTokens;

    decimal inCost = inputTokens / 1_000_000m * inputPrice;
    decimal outCost = outputTokens / 1_000_000m * outputPrice;
    decimal perCall = inCost + outCost;

    Console.WriteLine();
    Console.WriteLine($"Characters   : {text.Length}");
    Console.WriteLine($"Words        : {words}");
    Console.WriteLine($"Input tokens : {inputTokens}");
    Console.WriteLine($"Chars/token  : {(inputTokens == 0 ? 0 : (double)text.Length / inputTokens):F2}");
    Console.WriteLine($"Context used : {(double)(inputTokens + outputTokens) / ContextWindow:P3} of {ContextWindow:N0}");
    Console.WriteLine($"Cost per call: ${perCall:F6}  (input ${inCost:F6} + output ${outCost:F6})");
    Console.WriteLine($"Per 1,000 req: ${perCall * 1_000:F4}");

    var tokens = tokenizer.EncodeToTokens(text, out _);
    Console.WriteLine("\nToken breakdown (first 200):");
    Console.WriteLine(string.Join(" | ", tokens.Take(200).Select(t => t.Value)));
}

// ---------- 2) Temperature / softmax 
void TemperatureDemo()
{
     string[] words = { " Paris", " Lyon", " a", " beautiful", " Berlin" };
    double[] logits = { 8.0, 4.5, 3.0, 2.5, 1.0 };

    foreach (var t in new[] { 0.2, 0.7, 1.0, 1.5, 3.0 })
    {
        var probs = Softmax(logits, t);
        Console.WriteLine($"\nTemperature {t}:");
        for (int i = 0; i < words.Length; i++)
            Console.WriteLine($"  {words[i],-11} {probs[i],7:P2}  {new string('#', (int)(probs[i] * 40))}");
    }
}

// ---------- 3) Why long chats get expensive 
void ChatGrowthDemo()
{
    const int system = 200, userMsg = 50, reply = 150, turns = 10;
    long totalIn = 0, totalOut = 0;

    Console.WriteLine($"\nAssumptions: system={system}, user msg={userMsg}, reply={reply} tokens");
    Console.WriteLine("Turn | Input tokens sent | Cumulative input");
    for (int n = 1; n <= turns; n++)
    {
        int input = system + (n - 1) * (userMsg + reply) + userMsg;
        totalIn += input;
        totalOut += reply;
        Console.WriteLine($"{n,4} | {input,17} | {totalIn,16}");
    }

    decimal cost = totalIn / 1_000_000m * inputPrice + totalOut / 1_000_000m * outputPrice;
    Console.WriteLine($"\nTotal cost for {turns} turns: ${cost:F6}");
    Console.WriteLine($"If history were NOT resent: {(long)turns * (system + userMsg)} input tokens only.");
}

static double[] Softmax(double[] logits, double temperature)
{
    var scaled = logits.Select(l => l / temperature).ToArray();
    double max = scaled.Max();                        
    var exps = scaled.Select(s => Math.Exp(s - max)).ToArray();
    double sum = exps.Sum();
    return exps.Select(e => e / sum).ToArray();
}

static decimal ReadDecimal(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (decimal.TryParse(Console.ReadLine(), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) && v >= 0)
            return v;
        Console.WriteLine("Enter a number, e.g. 2.5");
    }
}