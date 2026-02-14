# Prompt Catalog

Icarus uses versioned system prompts for RAG, summarization, and other tasks. Prompts are stored in code and support variable substitution.

---

## Versioning Scheme

- **Format:** `major.minor` (e.g., `1.0`, `1.1`)
- **Major:** Breaking changes (e.g., new required variables, different behavior)
- **Minor:** Non-breaking tweaks (wording, examples)
- **Tracking:** Versions are defined in code and logged with responses for debugging

---

## Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `{{context}}` | Retrieved chunks concatenated for RAG | Chunk 1\n\nChunk 2\n\n... |
| `{{query}}` | User question or input | "What is the SSE format?" |
| `{{max_tokens}}` | Maximum tokens to generate | 1024 |
| `{{temperature}}` | Sampling temperature (0–2) | 0.7 |

Additional variables may be added per prompt (e.g., `{{format}}`, `{{language}}`).

---

## System Prompts

### RAG Response v1.0

Used for retrieval-augmented chat. Injects context and instructs the model to cite sources.

```
You are a helpful assistant for the Icarus knowledge base. Answer the user's question using only the provided context. If the context does not contain relevant information, say so. When you use information from the context, you may reference it implicitly. Keep answers concise and accurate.

Context:
{{context}}

User question: {{query}}

Respond in a helpful, professional tone. Maximum length: approximately {{max_tokens}} tokens. Use temperature {{temperature}} for natural variation.
```

### Summarization v1.0

Used for generating summaries of documents or search results.

```
You are a summarization assistant. Summarize the following content clearly and concisely. Preserve key facts, decisions, and recommendations. Do not add information not present in the source.

Content to summarize:
{{context}}

If a specific focus was requested: {{query}}

Keep the summary under {{max_tokens}} tokens. Use temperature {{temperature}}.
```

---

## Example Template with Variable Substitution

**Template:**

```
System: {{system_prompt}}

Context: {{context}}

User: {{query}}

Settings: max_tokens={{max_tokens}}, temperature={{temperature}}
```

**Substitution (pseudo-code):**

```csharp
var template = PromptCatalog.Get("rag-v1.0");
var filled = template
    .Replace("{{context}}", string.Join("\n\n", chunks.Select(c => c.Text)))
    .Replace("{{query}}", userQuery)
    .Replace("{{max_tokens}}", "1024")
    .Replace("{{temperature}}", "0.7");
```

**Result:**

```
System: You are a helpful assistant for the Icarus knowledge base...

Context: [Chunk 1 text]

[Chunk 2 text]

User: What is the SSE format?

Settings: max_tokens=1024, temperature=0.7
```

---

## Guidelines for Prompt Engineering in Icarus

### 1. Be Explicit About Context Usage

- Tell the model to use only provided context for RAG.
- Instruct it to say "I don't know" when context is insufficient.
- Reduces hallucination and off-topic answers.

### 2. Control Length and Style

- Use `{{max_tokens}}` and explicit instructions ("Keep answers concise").
- Specify tone (e.g., "professional", "conversational") when it matters.

### 3. Version All Changes

- Bump minor for wording tweaks; major for structural changes.
- Log prompt version with responses for debugging and A/B tests.

### 4. Test Edge Cases

- Empty context
- Very long context
- Ambiguous or multi-part queries
- Queries that should trigger "I don't know"

### 5. Avoid Sensitive Instructions in Prompts

- Do not hardcode API keys, internal URLs, or secrets.
- Use configuration or environment variables for deployment-specific values.

### 6. Optimize for Retrieval Quality

- Prompts cannot fix bad retrieval. Ensure chunks are relevant and well-chunked.
- If answers are off, improve chunking and retrieval before changing prompts.

### 7. Document Custom Variables

- When adding variables (e.g., `{{format}}`, `{{language}}`), document them in this catalog and in code.

---

## Prompt Registry (Code Reference)

Prompts are typically registered in a central catalog:

```csharp
// Example structure
public static class PromptCatalog
{
    public const string RagV1 = "rag-v1.0";
    public const string SummarizationV1 = "summarization-v1.0";

    public static string Get(string key) => key switch
    {
        RagV1 => Resources.Prompts.RagV1,
        SummarizationV1 => Resources.Prompts.SummarizationV1,
        _ => throw new ArgumentException($"Unknown prompt: {key}")
    };
}
```

Store template text in resource files or a dedicated `Prompts/` directory for easier editing and review.
