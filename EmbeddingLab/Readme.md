# Day 02: EmbeddingLab

A .NET console app that turns text into embeddings and ranks sentences by **cosine similarity**, with a relevance threshold. It is the semantic search core of a RAG pipeline, and the second project in my **.NET Developer to AI Engineer** journey.

> Goal: understand how meaning becomes numbers, how similarity is measured, and why a naive "top-K" search needs a relevance threshold.

---

## What it does

1. Embeds 20 sample sentences (cloud, cooking, cricket, finance, plus a paraphrase pair) using an Azure OpenAI embedding deployment.
2. Prints the vector dimensions, the first values of one vector, and its magnitude.
3. Accepts free-text queries, embeds each one, and ranks all sentences by cosine similarity.
4. Returns only results at or above a **threshold** (up to Top-K). If nothing passes, it prints "No relevant result" with the best score.

---

## Tech stack

- C# on .NET (console app, top-level statements)
- `Azure.AI.OpenAI` and `Microsoft.Extensions.AI.OpenAI`
- `IEmbeddingGenerator<string, Embedding<float>>`, a provider-neutral abstraction from Microsoft.Extensions.AI
- Cosine similarity implemented by hand to show the math

---

## Getting started

**Prerequisites:** .NET SDK 8 or later, and an Azure OpenAI **embedding** deployment (separate from a chat deployment).

Secrets come from environment variables and are never committed.

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://<your-resource>.openai.azure.com/"
$env:AZURE_OPENAI_KEY = "<your-key>"
$env:AZURE_OPENAI_EMBED_DEPLOYMENT = "<your-embedding-deployment-name>"

cd day02-embeddings-lab/EmbeddingLab
dotnet run
```

---

## How it works

```text
Startup:  20 sentences  ->  embedding model  ->  20 vectors (kept in memory)
Query:    question      ->  same embedding model -> 1 vector
Rank:     cosine(query, each doc)  ->  sort  ->  apply threshold  ->  Top-K
```

Cosine similarity measures the angle between two vectors:

```text
cos(A, B) = (A . B) / (|A| x |B|)
```

My deployment returns vectors with magnitude 1.0000, so cosine equals the dot product. This is why vector databases often use the cheaper dot product.

---

## Results

**Model:** `<your embedding deployment/model>`  |  **Dimensions:** 1536  |  **Doc 0 magnitude:** 1.0000

### Query experiments

| Query | Expected | Best score | Passed threshold? | Observation |
|---|---|---|---|---|
| How do I deploy a website to the cloud? | Azure App Service | 0.831 | Yes | Correct top result |
| What's the weather in Paris? | Nothing relevant | 0.743 | No | Irrelevant, yet scored high |
| recipe for bread | Dough / oven sentences | ... | ... | ... |
| who won the cricket game? | Match / series sentences | ... | ... | ... |
| my computer won't hold power | Battery / charge sentences | ... | ... | Paraphrase test with no shared keywords |
| best pizza in Rome | Nothing relevant | ... | ... | ... |

### Threshold finding

- With a first guess of **0.40**, every query returned five results, including the weather query. All scores sit between roughly 0.70 and 0.83, so a low threshold filters nothing.
- Good queries scored about **0.83** and irrelevant ones about **0.74**, so the separation is only about **0.09**.
- Threshold used: **...** (tuned from the table above).
- Raising the threshold removed the loosely related results for the cloud query (0.745 to 0.772), so there is a trade-off between **recall** (finding everything relevant) and **precision** (not returning junk).

---

## Key learnings

1. **Embeddings turn meaning into geometry.** Similar meaning gives nearby vectors, even with no shared words.
2. **Similarity scores are relative, not probabilities.** Some result always comes back, even when nothing is relevant.
3. **A threshold must be tuned per model and per dataset.** A value that works for one embedding model fails for another. Mine scores everything above about 0.70, so a threshold of 0.40 filtered nothing.
4. **Use the same embedding model for documents and queries,** and re-embed everything if the model changes.
5. **A single cutoff is fragile when scores are close.** Production systems add hybrid search (keyword + vector) and reranking.
6. **In RAG, the threshold acts as a gate.** If nothing passes, answer "I don't know" instead of sending weak context to the LLM.

---

## Known limitations

- Vectors are kept in memory and recomputed on every run. A real system stores them in a vector database.
- Only 20 short sentences, so the threshold I chose will not generalize without a larger labelled test set.
- Embeddings are weak at negation, exact IDs, and numbers. Hybrid search addresses this.
- Documents are short sentences. Real documents need chunking.

---

## Security notes

- The API key is read from environment variables and is not in source control.
- Stored embeddings can leak information about the original text, so treat them as sensitive data.

---

## Project structure

```text
day02-embeddings-lab/
├── README.md
└── EmbeddingLab/
    ├── Program.cs
    └── EmbeddingLab.csproj
```

---

## Journey

Part of an 84-day roadmap covering LLM fundamentals, Microsoft.Extensions.AI, RAG, agents, MCP, evaluation, security, observability, and production deployment.

**Previous:** Day 01, TokenLab (tokens, cost, temperature)
**Next:** Day 03, the `IChatClient` abstraction and a chat app on Azure OpenAI
