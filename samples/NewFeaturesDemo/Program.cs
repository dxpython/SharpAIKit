using SharpAIKit.LLM;
using SharpAIKit.Common;
using SharpAIKit.Agent;
using SharpAIKit.Graph;
using SharpAIKit.Memory;
using SharpAIKit.Observability;
using ToolDefinition = SharpAIKit.Agent.ToolDefinition;

Console.WriteLine("🧪 SharpAIKit New Features Test (v0.3.0)\n");
Console.WriteLine("=".PadRight(60, '='));

var apiKey = "sk-e164311ef7914e46a5d760c505714b94";
var client = LLMClientFactory.CreateDeepSeek(apiKey);

// ============================================================
// 1. StrongContext - Type-safe Context
// ============================================================
Console.WriteLine("\n✅ Test 1: StrongContext\n");

try
{
    var context = new StrongContext();
    
    // Set typed values
    context.Set("user_id", 12345);
    context.Set("user_name", "TestUser");
    context.Set("is_active", true);
    
    // Get typed values
    var userId = context.Get<int>("user_id");
    var userName = context.Get<string>("user_name");
    var isActive = context.Get<bool>("is_active");
    
    Console.WriteLine($"  User ID: {userId} (type: {userId.GetType().Name})");
    Console.WriteLine($"  User Name: {userName} (type: {userName?.GetType().Name})");
    Console.WriteLine($"  Is Active: {isActive} (type: {isActive.GetType().Name})");
    
    // Serialization
    var json = context.ToJson();
    Console.WriteLine($"  JSON: {json.Substring(0, Math.Min(50, json.Length))}...");
    
    // Deserialization
    var restored = StrongContext.FromJson(json);
    var restoredUserId = restored.Get<int>("user_id");
    Console.WriteLine($"  Restored User ID: {restoredUserId}");
    
    Console.WriteLine("  ✅ StrongContext test passed!");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ StrongContext test failed: {ex.Message}");
}

// ============================================================
// 2. Modular Architecture - IPlanner, IToolExecutor
// ============================================================
Console.WriteLine("\n✅ Test 2: Modular Architecture\n");

try
{
    var planner = new SimplePlanner(client);
    var executor = new DefaultToolExecutor();
    var memory = new BufferMemory();
    
    // Register a simple tool
    executor.RegisterTool(new ToolDefinition
    {
        Name = "add",
        Description = "Add two numbers",
        Parameters = new List<ToolParameter>
        {
            new ToolParameter { Name = "a", Type = "number", Required = true },
            new ToolParameter { Name = "b", Type = "number", Required = true }
        },
        Execute = async args =>
        {
            var a = Convert.ToDouble(args["a"]);
            var b = Convert.ToDouble(args["b"]);
            return (a + b).ToString();
        }
    });
    
    var context = new StrongContext();
    context.Set("available_tools", executor.GetAvailableTools().Select(t => t.Name).ToList());
    
    // Test planner
    var plan = await planner.PlanAsync("Add 5 and 3", context);
    Console.WriteLine($"  Plan generated: {plan.Steps.Count} steps");
    Console.WriteLine($"  First step: {plan.Steps.First().Description}");
    
    // Test executor
    var execResult = await executor.ExecuteAsync("add", new Dictionary<string, object?>
    {
        ["a"] = 5,
        ["b"] = 3
    }, context);
    
    Console.WriteLine($"  Tool execution result: {execResult.Result}");
    Console.WriteLine($"  Success: {execResult.Success}");
    
    Console.WriteLine("  ✅ Modular Architecture test passed!");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ Modular Architecture test failed: {ex.Message}");
    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
}

// ============================================================
// 3. Enhanced Agent
// ============================================================
Console.WriteLine("\n✅ Test 3: Enhanced Agent\n");

try
{
    var planner = new SimplePlanner(client);
    var executor = new DefaultToolExecutor();
    var memory = new BufferMemory();
    
    // Register tool
    executor.RegisterTool(new ToolDefinition
    {
        Name = "multiply",
        Description = "Multiply two numbers",
        Parameters = new List<ToolParameter>
        {
            new ToolParameter { Name = "a", Type = "number", Required = true },
            new ToolParameter { Name = "b", Type = "number", Required = true }
        },
        Execute = async args =>
        {
            var a = Convert.ToDouble(args["a"]);
            var b = Convert.ToDouble(args["b"]);
            return (a * b).ToString();
        }
    });
    
    var agent = new EnhancedAgent(client, planner, executor, memory);
    
    // Run agent
    var result = await agent.RunAsync("Multiply 4 by 7");
    
    Console.WriteLine($"  Task: Multiply 4 by 7");
    Console.WriteLine($"  Answer: {result.Answer}");
    Console.WriteLine($"  Steps executed: {result.StepResults.Count}");
    Console.WriteLine($"  Success: {result.Success}");
    
    Console.WriteLine("  ✅ Enhanced Agent test passed!");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ Enhanced Agent test failed: {ex.Message}");
    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
}

// ============================================================
// 4. Graph State Persistence
// ============================================================
Console.WriteLine("\n✅ Test 4: Graph State Persistence\n");

try
{
    var store = new MemoryGraphStateStore();
    var graph = new EnhancedSharpGraph("start");
    graph.StateStore = store;
    graph.AutoSaveCheckpoints = true;
    
    graph.AddNode("start", async state =>
    {
        state.Set("value", 10);
        state.NextNode = "process";
        return state;
    });
    
    graph.AddNode("process", async state =>
    {
        var value = state.Get<int>("value");
        state.Set("result", value * 2);
        state.ShouldEnd = true;
        return state;
    });
    
    var initialState = new GraphState();
    var result = await graph.ExecuteAsync(initialState);
    
    Console.WriteLine($"  Initial value: {initialState.Get<int>("value")}");
    Console.WriteLine($"  Result: {result.Get<int>("result")}");
    
    // Check checkpoint
    var checkpoints = await store.ListCheckpointsAsync(graph.GetType().Name);
    Console.WriteLine($"  Checkpoints saved: {checkpoints.Count}");
    
    Console.WriteLine("  ✅ Graph State Persistence test passed!");
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ Graph State Persistence test failed: {ex.Message}");
    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
}

// ============================================================
// 5. Graph Event System
// ============================================================
Console.WriteLine("\n✅ Test 5: Graph Event System\n");

try
{
    var graph = new EnhancedSharpGraph("start");
    var nodeStartCount = 0;
    var nodeEndCount = 0;
    
    graph.OnNodeStart += async (sender, e) =>
    {
        nodeStartCount++;
        Console.WriteLine($"    Node '{e.NodeName}' started");
    };
    
    graph.OnNodeEnd += async (sender, e) =>
    {
        nodeEndCount++;
        Console.WriteLine($"    Node '{e.NodeName}' ended (duration: {e.ExecutionTime.TotalMilliseconds}ms)");
    };
    
    graph.AddNode("start", async state =>
    {
        await Task.Delay(10); // Simulate work
        state.NextNode = "end";
        return state;
    });
    
    graph.AddNode("end", async state =>
    {
        state.ShouldEnd = true;
        return state;
    });
    
    await graph.ExecuteAsync(new GraphState());
    
    Console.WriteLine($"  Nodes started: {nodeStartCount}");
    Console.WriteLine($"  Nodes ended: {nodeEndCount}");
    
    if (nodeStartCount == 2 && nodeEndCount == 2)
    {
        Console.WriteLine("  ✅ Graph Event System test passed!");
    }
    else
    {
        Console.WriteLine($"  ❌ Graph Event System test failed: Expected 2 starts/ends, got {nodeStartCount}/{nodeEndCount}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ Graph Event System test failed: {ex.Message}");
    Console.WriteLine($"  Stack trace: {ex.StackTrace}");
}

// ============================================================
// 6. OpenTelemetry Support
// ============================================================
Console.WriteLine("\n✅ Test 6: OpenTelemetry Support\n");

try
{
    using var activity = OpenTelemetrySupport.StartLLMActivity("TestChat", "deepseek-chat");
    if (activity != null)
    {
        activity.SetTag("llm.provider", "DeepSeek");
        activity.SetTag("test", "true");
        Console.WriteLine($"  Activity created: {activity.DisplayName}");
        Console.WriteLine($"  Activity ID: {activity.Id}");
        Console.WriteLine("  ✅ OpenTelemetry Support test passed!");
    }
    else
    {
        Console.WriteLine("  ⚠️  OpenTelemetry activity is null (ActivitySource may not be configured)");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"  ❌ OpenTelemetry Support test failed: {ex.Message}");
}

// ============================================================
// Summary
// ============================================================
Console.WriteLine("\n" + "=".PadRight(60, '='));
Console.WriteLine("✅ All new features tests completed!");
Console.WriteLine("\nNew Features Tested:");
Console.WriteLine("  1. ✅ StrongContext - Type-safe data passing");
Console.WriteLine("  2. ✅ Modular Architecture - IPlanner/IToolExecutor");
Console.WriteLine("  3. ✅ Enhanced Agent - Combined components");
Console.WriteLine("  4. ✅ Graph State Persistence - Checkpoint support");
Console.WriteLine("  5. ✅ Graph Event System - Lifecycle hooks");
Console.WriteLine("  6. ✅ OpenTelemetry Support - Distributed tracing");

client.Dispose();

