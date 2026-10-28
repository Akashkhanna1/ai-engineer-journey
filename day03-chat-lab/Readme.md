# Day 03: ChatLab

A .NET console chat app built on the provider-neutral **`IChatClient`** abstraction from `Microsoft.Extensions.AI`, backed by Azure OpenAI. It keeps conversation history, sets a system prompt, and prints token usage for every turn. It is the third project in my **.NET Developer to AI Engineer** journey.

> Goal: understand that a chat model is stateless, that the application owns the conversation, and how to code against an abstraction instead of a provider SDK.

---

## What it does

- Sends chat messages to an Azure OpenAI deployment through `IChatClient`.
- Uses a **system prompt** to control behavior and a `List<ChatMessage>` to hold history.
- Prints input tokens, output tokens, session totals, message count, and the **finish reason** after every reply.
- Warns when output is cut off (`FinishReason == Length`).
- Commands: `reset` clears the history (keeping the system prompt), `exit` quits.
- If a call fails, the failed user message is removed from history so the conversation stays consistent.

---

## Tech stack

- C# on .NET (console app, top-level statements)
- `Azure.AI.OpenAI` and `Microsoft.Extensions.AI.OpenAI`
- `IChatClient`, `ChatMessage`, `ChatRole`, `ChatOptions`, `ChatResponse` from Microsoft.Extensions.AI

---

## Getting started

**Prerequisites:** .NET SDK 8 or later, and an Azure OpenAI **chat** deployment (for example `gpt-4o-mini`). Use the **deployment name**, not the model name.

Secrets come from environment variables and are never committed.

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://<your-resource>.openai.azure.com/"
$env:AZURE_OPENAI_KEY = "<your-key>"
$env:AZURE_OPENAI_CHAT_DEPLOYMENT = "<your-chat-deployment-name>"

cd day03-chat-lab/ChatLab
dotnet run
```

---

## How it works

```text
history = [ system prompt ]
loop:
    history.add(user message)
    response = client.GetResponseAsync(history, options)   // sends the WHOLE history
    history.add(assistant reply)                           // the app must store it
```

Only one line of startup code knows about Azure:

```csharp
IChatClient client = new AzureOpenAIClient(...).GetChatClient(deployment).AsIChatClient();
```

Everything after it depends only on `IChatClient`, so the provider can be swapped (for example to a local model) or replaced with a fake in tests.

---

## Results

**Deployment/model:** `<your chat deployment/model>`  |  **Temperature:** 0.3  |  **MaxOutputTokens:** 500

### Experiment 1 and 2: statelessness

| Step | Result |
|---|---|
| Said "My name is Akash", then "What is my name?" | ... |
| Typed `reset`, then asked "What is my name?" | ... |

Conclusion: the model remembered only while my app resent the history. After `reset`, the information was gone, so the memory lives in the application, not in the model or the client.

### Experiment 3: history growth

| Turn | Input tokens (`in=`) | Output tokens (`out=`) |
|---|---|---|
| 1 | ... | ... |
| 2 | ... | ... |
| 3 | ... | ... |
| 4 | ... | ... |
| 5 | ... | ... |
| 6 | ... | ... |

Conclusion: input tokens rise every turn because the full history is resent, even when each new message is short.

### Experiment 4: truncated output

- Set `MaxOutputTokens = 20` and asked for a long explanation.
- Result: ... (finish reason shown: ...)

### Experiment 5: temperature

| Temperature | Same question, 3 runs | Variation |
|---|---|---|
| 0 | ... | ... |
| 1.2 | ... | ... |

### Experiment 6: system prompt

- With vs without the system prompt: ...

---

## Key learnings

1. **`IChatClient` is a provider-neutral abstraction.** It uses the adapter pattern, works with dependency injection, and supports middleware (logging, caching, retries).
2. **The model is stateless.** The application owns conversation history and must resend it, including the assistant's own replies, on every call.
3. **Cost grows with every turn,** because each request carries the entire history.
4. **Always check `FinishReason`.** `Length` means the answer was cut off, which can break JSON or other structured output.
5. **`MaxOutputTokens` protects cost and latency.** Size it to the largest answer you expect, and remember input plus output must fit in the context window.
6. **On Azure, call the deployment name,** not the model name.
7. **In production, persist history per conversation** in a database or Redis, not in process memory.

---

## Known limitations

- History grows without limit, so long chats get expensive and will eventually exceed the context window (trimming and summarizing come later).
- History lives in process memory and is lost when the app exits.
- Responses are not streamed. The full answer appears at once (streaming is Day 4).
- No retry, logging, or caching middleware yet (Day 4).
- Token usage depends on the provider returning usage data.

---

## Security notes

- API key is read from environment variables and is not in source control.
- Production should use Key Vault with managed identity instead of keys (planned for Week 11).

---

## Project structure

```text
day03-chat-lab/
├── README.md
└── ChatLab/
    ├── Program.cs
    └── ChatLab.csproj
```

---

## Journey

Part of an 84-day roadmap covering LLM fundamentals, Microsoft.Extensions.AI, RAG, agents, MCP, evaluation, security, observability, and production deployment.

**Previous:** Day 02, EmbeddingLab (embeddings, cosine similarity, threshold)
**Next:** Day 04, streaming, dependency injection, and middleware
