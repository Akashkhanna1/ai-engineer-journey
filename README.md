# Day 01: TokenLab

A .NET console app that makes LLM tokens, context windows, temperature, and cost visible. It is the first project in my **.NET Developer to AI Engineer** journey.

> Goal: understand how LLM cost, latency, and context limits behave **before** building AI features on top of them.

---

## What it does

| Menu option | Feature | What it teaches |
|---|---|---|
| **1** | **Token and cost analyzer.** Paste any text to see characters, words, tokens, chars/token, context window usage, cost per call and cost per 1,000 requests | Tokens, not words, drive cost and limits |
| **2** | **Temperature demo (softmax).** Shows how the next-token probability distribution changes at temperature 0.2, 0.7, 1.0, 1.5 and 3.0 | What temperature really does |
| **3** | **Chat history cost growth.** Simulates 10 turns of a chat and shows input tokens growing every turn | The model is stateless, so history is resent each time |

---

## Tech stack

- C# on .NET (console app, top-level statements)
- [`Microsoft.ML.Tokenizers`](https://www.nuget.org/packages/Microsoft.ML.Tokenizers): official Microsoft tokenizer library
- `Microsoft.ML.Tokenizers.Data.O200kBase`: vocabulary used by the GPT-4o model family (`o200k_base`)

No API key or network call is needed. Everything runs locally.

---

## Getting started

**Prerequisites:** .NET SDK 8 or later.

```bash
git clone <your-repo-url>
cd ai-engineer-journey/TokenLab
dotnet restore
dotnet run
```

On startup the app asks for two prices in USD per 1M tokens (input and output). Take them from the Azure OpenAI pricing page for the model you use. Prices change, so none are hard-coded.

### Sample session

```text
=== TokenLab ===
Input  price per 1M tokens (USD): <your input price>
Output price per 1M tokens (USD): <your output price>

1) Analyze text and estimate cost
2) Temperature demo (softmax)
3) Chat history cost growth
0) Exit
```

---

## My observations

> Replace the placeholders with your own measured results. Specific numbers are what make this README stand out.

### Tokens: different text, different cost

| Input | Characters | Tokens | Chars/token | Observation |
|---|---|---|---|---|
| `Hello world` | ... | ... | ... | Short English text is cheap |
| English sentence | ... | ... | ... | Baseline |
| Same sentence in Hindi | ... | ... | ... | Non-English text needs more tokens, so it costs more |
| Code or stack trace | ... | ... | ... | Symbols and identifiers split into many tokens |
| JSON snippet | ... | ... | ... | Punctuation and keys add tokens |
| Same data as plain prose | ... | ... | ... | Compare with the JSON row |

### Temperature

- At **0.2** the top token (" Paris") dominates, which suits extraction, classification and JSON.
- At **3.0** the distribution is much flatter, which gives varied but less reliable output.

### Chat history growth

- Turn 1 sends ... input tokens. Turn 10 sends ... input tokens.
- Total input tokens over 10 turns: ... compared with ... if history were not resent.
- Conclusion: cost grows faster than the number of turns, because every turn resends all previous turns.

---

## Key learnings

1. **Tokens drive cost, context, and latency.** Output tokens are usually priced higher than input tokens.
2. **The context window covers input and output together.** Reserve room for the reply.
3. **LLMs are stateless.** The application resends conversation history on every call.
4. **Temperature rescales the probabilities before sampling.** Use low values for extraction and structured output, and higher values for creative work. Even 0 does not guarantee identical output.
5. **Count tokens with the tokenizer that matches the model.** Do not estimate from word count.

---

## Cost-control ideas for production

- Count tokens before sending a request, and set `max_tokens`.
- Trim or summarize chat history instead of resending all of it.
- Retrieve fewer, better chunks (RAG) instead of pasting whole documents.
- Cache repeated requests, and route simple tasks to a cheaper model.
- Log tokens and cost per request and per user (built in Week 1 of the roadmap).

---

## Known limitations

- The analyzer reads a single line of text at a time.
- Token counts are exact only for models that use the `o200k_base` vocabulary. Other model families tokenize differently.
- The temperature demo uses made-up logits to illustrate the idea, not real model output.
- The chat growth simulation uses fixed assumed message sizes.

---

## Project structure

```text
day01-token-lab/
├── README.md
└── TokenLab/
    ├── Program.cs
    └── TokenLab.csproj
```

---


