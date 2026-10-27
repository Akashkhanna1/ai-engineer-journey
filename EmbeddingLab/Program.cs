using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

string endpoint = "Azure Endpoint";
string key = "Azure Key";
string deployment = "text-embedding-ada-002-example";

const float Threshold = 0.78f;   // starting guess, tune it from your own experiments
const int TopK = 5;

IEmbeddingGenerator<string, Embedding<float>> generator =
    new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
        .GetEmbeddingClient(deployment)
        .AsIEmbeddingGenerator();

string[] docs =
{
    // Cloud
    "Azure App Service lets you host web apps without managing servers.",
    "We migrated our on-premises SQL Server database to Azure SQL.",
    "Containers package an application with its dependencies so it runs anywhere.",
    "Use managed identity so your app never stores passwords in config files.",
    "Autoscaling adds more instances when traffic increases.",

    // Cooking
    "Simmer the tomatoes with garlic and basil for twenty minutes.",
    "Knead the dough until it becomes smooth and elastic.",
    "Add a pinch of salt to bring out the sweetness of the dessert.",
    "Marinate the chicken overnight in yogurt and spices.",
    "Preheat the oven before baking the bread.",

    // Cricket
    "The batsman hit a six off the last ball to win the match.",
    "The bowler took five wickets in the second innings.",
    "India won the series after a thrilling final over.",
    "The captain set an attacking field for the spinner.",

    // Finance
    "The central bank raised interest rates to control inflation.",
    "Diversifying your portfolio reduces investment risk.",
    "Her credit card statement shows a late payment fee.",
    "Stock prices fell sharply after the earnings report.",

    // Same meaning, almost no shared words
    "My laptop battery drains very quickly.",
    "The device loses charge within an hour."
};

Console.WriteLine($"Embedding {docs.Length} sentences...");
var generated = await generator.GenerateAsync(docs);
float[][] docVectors = generated.Select(e => e.Vector.ToArray()).ToArray();

Console.WriteLine($"Vector dimensions: {docVectors[0].Length}");
Console.WriteLine($"First 5 values of doc 0: {string.Join(", ", docVectors[0].Take(5).Select(v => v.ToString("F4")))}");
Console.WriteLine($"Length (magnitude) of doc 0 vector: {Math.Sqrt(docVectors[0].Sum(v => v * v)):F4}");

while (true)
{
    Console.Write("\nQuery (empty to quit): ");
    var query = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(query)) break;

    var queryResult = await generator.GenerateAsync(new[] { query });
    float[] queryVector = queryResult[0].Vector.ToArray();

    // Score every document, best first
    var scored = docs
        .Select((text, i) => (Text: text, Score: Cosine(queryVector, docVectors[i])))
        .OrderByDescending(x => x.Score)
        .ToList();

    // Keep only results at or above the threshold, up to TopK
    var relevant = scored
        .Where(x => x.Score >= Threshold)
        .Take(TopK)
        .ToList();

    if (relevant.Count == 0)
    {
        Console.WriteLine($"No relevant result (best score {scored[0].Score:F3}, threshold {Threshold:F2}).");
        continue;
    }

    Console.WriteLine("Top results:");
    foreach (var (text, score) in relevant)
        Console.WriteLine($"  {score:F3}  {text}");
}

static float Cosine(float[] a, float[] b)
{
    float dot = 0, normA = 0, normB = 0;
    for (int i = 0; i < a.Length; i++)
    {
        dot += a[i] * b[i];
        normA += a[i] * a[i];
        normB += b[i] * b[i];
    }
    return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
}

static string Env(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Set the {name} environment variable.");