using SharpAIKit.Common;
using SharpAIKit.LLM;

namespace SharpAIKit.Graph;

/// <summary>
/// Pre-built graph templates for common patterns.
/// </summary>
public static class GraphTemplates
{
    /// <summary>
    /// Creates a ReAct (Reasoning + Acting) pattern graph.
    /// </summary>
    public static EnhancedSharpGraph CreateReActPattern(ILLMClient llmClient, List<ToolDefinition> tools)
    {
        var builder = new EnhancedSharpGraphBuilder("think", maxIterations: 20);

        builder
            .Node("think", async state =>
            {
                var task = state.Get<string>("task") ?? "";
                var history = state.Get<List<string>>("history") ?? new List<string>();
                
                var prompt = $@"Task: {task}

            Previous steps:
            {string.Join("\n", history)}

            Think about what to do next. If you need to use a tool, respond with JSON:
            {{""action"": ""tool"", ""tool"": ""tool_name"", ""args"": {{""param"": ""value""}}}}

            If you can answer directly, respond with JSON:
            {{""action"": ""answer"", ""content"": ""your answer""}}";

                var response = await llmClient.ChatAsync(prompt);
                state.Set("llm_response", response);
                return state;
            })
            .Node("parse", async state =>
            {
                var response = state.Get<string>("llm_response") ?? "";
                // Parse JSON response
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(response);
                    var action = json.RootElement.GetProperty("action").GetString();
                    state.Set("action", action);
                    
                    if (action == "tool")
                    {
                        state.Set("tool_name", json.RootElement.GetProperty("tool").GetString());
                        state.NextNode = "execute_tool";
                    }
                    else if (action == "answer")
                    {
                        state.Set("answer", json.RootElement.GetProperty("content").GetString());
                        state.NextNode = "finalize";
                    }
                }
                catch
                {
                    state.NextNode = "think"; // Retry
                }
                return state;
            })
            .Node("execute_tool", async state =>
            {
                var toolName = state.Get<string>("tool_name") ?? "";
                // Execute tool (simplified)
                state.Set("tool_result", $"Tool {toolName} executed");
                state.NextNode = "think";
                return state;
            })
            .Node("finalize", async state =>
            {
                state.Output = state.Get<string>("answer") ?? "";
                state.ShouldEnd = true;
                return state;
            })
            .Edge("think", "parse")
            .Edge("parse", "execute_tool", condition: s => s.Get<string>("action") == "tool")
            .Edge("parse", "finalize", condition: s => s.Get<string>("action") == "answer")
            .Edge("execute_tool", "think");

        return builder.Build();
    }

    /// <summary>
    /// Creates a MapReduce pattern graph for processing multiple documents.
    /// </summary>
    public static EnhancedSharpGraph CreateMapReducePattern(ILLMClient llmClient, List<string> documents)
    {
        var builder = new EnhancedSharpGraphBuilder("start", maxIterations: 100);

        builder
            .Node("start", async state =>
            {
                state.Set("documents", documents);
                state.Set("results", new List<string>());
                state.NextNode = "map";
                return state;
            })
            .Fork("map", documents.Select((_, i) => $"process_doc_{i}").ToArray())
            .Node("process_doc_0", async state =>
            {
                // Process first document (simplified)
                var docs = state.Get<List<string>>("documents") ?? new List<string>();
                if (docs.Count > 0)
                {
                    var result = await llmClient.ChatAsync($"Process this document: {docs[0]}");
                    var results = state.Get<List<string>>("results") ?? new List<string>();
                    results.Add(result);
                    state.Set("results", results);
                }
                return state;
            })
            .Join("reduce", JoinStrategy.All, states =>
            {
                var allResults = new List<string>();
                foreach (var branchState in states)
                {
                    var results = branchState.Get<List<string>>("results") ?? new List<string>();
                    allResults.AddRange(results);
                }
                var finalState = states.First();
                finalState.Set("all_results", allResults);
                finalState.NextNode = "finalize";
                return finalState;
            })
            .Node("finalize", async state =>
            {
                var allResults = state.Get<List<string>>("all_results") ?? new List<string>();
                var summary = await llmClient.ChatAsync("Summarize these results: " + string.Join("\n", allResults));
                state.Output = summary;
                state.ShouldEnd = true;
                return state;
            })
            .Edge("start", "map")
            .Edge("map", "reduce")
            .Edge("reduce", "finalize");

        return builder.Build();
    }

    /// <summary>
    /// Creates a Reflection pattern graph (self-correcting).
    /// </summary>
    public static EnhancedSharpGraph CreateReflectionPattern(ILLMClient llmClient)
    {
        var builder = new EnhancedSharpGraphBuilder("generate", maxIterations: 10);

        builder
            .Node("generate", async state =>
            {
                var task = state.Get<string>("task") ?? "";
                var response = await llmClient.ChatAsync($"Complete this task: {task}");
                state.Set("initial_response", response);
                state.Set("attempts", 1);
                state.NextNode = "reflect";
                return state;
            })
            .Node("reflect", async state =>
            {
                var task = state.Get<string>("task") ?? "";
                var response = state.Get<string>("initial_response") ?? "";
                
                var reflection = await llmClient.ChatAsync($"""
                    Task: {task}
                    Initial response: {response}
                    
                    Critically evaluate this response. Is it correct? What could be improved?
                    """);
                
                state.Set("reflection", reflection);
                
                // Check if reflection suggests improvement
                if (reflection.Contains("improve") || reflection.Contains("incorrect"))
                {
                    state.NextNode = "refine";
                }
                else
                {
                    state.NextNode = "finalize";
                }
                return state;
            })
            .Node("refine", async state =>
            {
                var task = state.Get<string>("task") ?? "";
                var initialResponse = state.Get<string>("initial_response") ?? "";
                var reflection = state.Get<string>("reflection") ?? "";
                var attempts = state.Get<int>("attempts") + 1;
                
                if (attempts > 3)
                {
                    state.Output = initialResponse; // Use best attempt
                    state.NextNode = "finalize";
                }
                else
                {
                    var refined = await llmClient.ChatAsync($"""
                        Task: {task}
                        Previous attempt: {initialResponse}
                        Reflection: {reflection}
                        
                        Provide an improved response:
                        """);
                    
                    state.Set("initial_response", refined);
                    state.Set("attempts", attempts);
                    state.NextNode = "reflect"; // Loop back
                }
                return state;
            })
            .Node("finalize", async state =>
            {
                state.Output = state.Get<string>("initial_response") ?? "";
                state.ShouldEnd = true;
                return state;
            })
            .Edge("generate", "reflect")
            .Edge("reflect", "refine", condition: s => s.Get<string>("reflection")?.Contains("improve") == true)
            .Edge("reflect", "finalize", condition: s => s.Get<string>("reflection")?.Contains("improve") != true)
            .Edge("refine", "reflect");

        return builder.Build();
    }
}

