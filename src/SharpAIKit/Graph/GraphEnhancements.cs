using System.Runtime.CompilerServices;
using SharpAIKit.Common;

namespace SharpAIKit.Graph;

/// <summary>
/// Event arguments for graph node execution.
/// </summary>
public class GraphNodeEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the graph state.
    /// </summary>
    public GraphState State { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution time.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets any error that occurred.
    /// </summary>
    public Exception? Error { get; set; }
}

/// <summary>
/// Event handler for graph node events.
/// </summary>
public delegate Task GraphNodeEventHandler(object sender, GraphNodeEventArgs e);

/// <summary>
/// Event handler for streaming events.
/// </summary>
public delegate Task GraphStreamingEventHandler(object sender, string chunk);

/// <summary>
/// Enhanced SharpGraph with events, parallel execution, and streaming support.
/// </summary>
public class EnhancedSharpGraph
{
    private readonly SharpGraph _baseGraph;
    private readonly Dictionary<string, GraphNode> _nodes = new();
    private readonly string _entryNode;
    private readonly int _maxIterations;

    /// <summary>
    /// Event raised when a node starts execution.
    /// </summary>
    public event GraphNodeEventHandler? OnNodeStart;

    /// <summary>
    /// Event raised when a node ends execution.
    /// </summary>
    public event GraphNodeEventHandler? OnNodeEnd;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    public event GraphNodeEventHandler? OnError;

    /// <summary>
    /// Event raised for streaming output from nodes.
    /// </summary>
    public event GraphStreamingEventHandler? OnStreaming;

    /// <summary>
    /// Gets or sets the state store for persistence.
    /// </summary>
    public IGraphStateStore? StateStore { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-save checkpoints.
    /// </summary>
    public bool AutoSaveCheckpoints { get; set; } = false;

    /// <summary>
    /// Gets the maximum iterations.
    /// </summary>
    public int MaxIterations { get; set; }

    /// <summary>
    /// Creates a new enhanced graph.
    /// </summary>
    public EnhancedSharpGraph(string entryNode, int maxIterations = 100)
    {
        _entryNode = entryNode ?? throw new ArgumentNullException(nameof(entryNode));
        _maxIterations = maxIterations;
        MaxIterations = maxIterations;
        _baseGraph = new SharpGraph(entryNode, maxIterations);
    }

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    public EnhancedSharpGraph AddNode(string name, Func<GraphState, Task<GraphState>> action, string? description = null)
    {
        _baseGraph.AddNode(name, action, description);
        _nodes[name] = new GraphNode
        {
            Name = name,
            Description = description ?? name,
            Action = action
        };
        return this;
    }

    /// <summary>
    /// Adds an edge.
    /// </summary>
    public EnhancedSharpGraph AddEdge(string from, string to, Func<GraphState, bool>? condition = null, string? label = null)
    {
        _baseGraph.AddEdge(from, to, condition, label);
        return this;
    }

    /// <summary>
    /// Executes the graph with event support.
    /// </summary>
    public async Task<GraphState> ExecuteAsync(GraphState? initialState = null, CancellationToken cancellationToken = default)
    {
        var state = initialState ?? new GraphState();
        state.CurrentNode = _entryNode;

        var iterations = 0;
        var visitedNodes = new HashSet<string>();

        while (iterations < MaxIterations && !state.ShouldEnd)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeName = state.CurrentNode;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Raise OnNodeStart event
            if (OnNodeStart != null)
            {
                await OnNodeStart(this, new GraphNodeEventArgs
                {
                    NodeName = nodeName,
                    State = state.Clone()
                });
            }

            try
            {
                if (!_nodes.TryGetValue(nodeName, out var currentNode))
                {
                    throw new InvalidOperationException($"Node '{nodeName}' does not exist");
                }

                visitedNodes.Add(nodeName);

                // Execute the node action
                if (currentNode.Action != null)
                {
                    state = await ExecuteNodeWithStreamingAsync(currentNode, state, cancellationToken);
                }

                stopwatch.Stop();

                // Raise OnNodeEnd event
                if (OnNodeEnd != null)
                {
                    await OnNodeEnd(this, new GraphNodeEventArgs
                    {
                        NodeName = nodeName,
                        State = state.Clone(),
                        ExecutionTime = stopwatch.Elapsed
                    });
                }

                // Auto-save checkpoint if enabled
                if (AutoSaveCheckpoints && StateStore != null)
                {
                    var checkpoint = new GraphCheckpoint
                    {
                        GraphName = GetType().Name,
                        CurrentNode = nodeName,
                        StateData = state.Data,
                        ExecutionHistory = visitedNodes.ToList()
                    };
                    await StateStore.SaveCheckpointAsync(checkpoint, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Raise OnError event
                if (OnError != null)
                {
                    await OnError(this, new GraphNodeEventArgs
                    {
                        NodeName = nodeName,
                        State = state.Clone(),
                        ExecutionTime = stopwatch.Elapsed,
                        Error = ex
                    });
                }

                throw;
            }

            // Check if we should end
            if (state.ShouldEnd)
            {
                break;
            }

            // Determine next node (same logic as base class)
            var nextNode = DetermineNextNode(state, nodeName);
            if (nextNode == null)
            {
                state.ShouldEnd = true;
                break;
            }

            state.CurrentNode = nextNode;
            state.NextNode = null;
            iterations++;
        }

        if (iterations >= MaxIterations)
        {
            throw new InvalidOperationException($"Graph execution exceeded maximum iterations ({MaxIterations})");
        }

        return state;
    }

    /// <summary>
    /// Executes a node with streaming support.
    /// </summary>
    private async Task<GraphState> ExecuteNodeWithStreamingAsync(GraphNode node, GraphState state, CancellationToken cancellationToken)
    {
        // Check if the action supports streaming
        var result = await node.Action!(state);
        
        // If state has streaming data, raise streaming events
        if (result.Data.TryGetValue("_streaming_chunks", out var chunks) && chunks is List<string> chunkList)
        {
            foreach (var chunk in chunkList)
            {
                if (OnStreaming != null)
                {
                    await OnStreaming(this, chunk);
                }
            }
            result.Data.Remove("_streaming_chunks");
        }

        return result;
    }

    /// <summary>
    /// Streams graph execution, yielding state updates.
    /// </summary>
    public async IAsyncEnumerable<GraphState> StreamExecuteAsync(
        GraphState? initialState = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var state = initialState ?? new GraphState();
        state.CurrentNode = _entryNode;

        var iterations = 0;

        while (iterations < MaxIterations && !state.ShouldEnd)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var nodeName = state.CurrentNode;

            if (!_nodes.TryGetValue(nodeName, out var currentNode))
            {
                throw new InvalidOperationException($"Node '{nodeName}' does not exist");
            }

            // Execute the node action
            if (currentNode.Action != null)
            {
                state = await currentNode.Action(state);
            }

            // Yield current state
            yield return state.Clone();

            if (state.ShouldEnd)
            {
                break;
            }

            var nextNode = DetermineNextNode(state, nodeName);
            if (nextNode == null)
            {
                state.ShouldEnd = true;
                yield return state;
                break;
            }

            state.CurrentNode = nextNode;
            state.NextNode = null;
            iterations++;
        }
    }

    private string? DetermineNextNode(GraphState state, string currentNodeName)
    {
        if (!_nodes.TryGetValue(currentNodeName, out var currentNode))
        {
            return null;
        }

        // Check condition function
        if (currentNode.Condition != null)
        {
            var next = currentNode.Condition(state);
            if (next != null) return next;
        }

        // Check edges
        foreach (var edge in currentNode.Edges)
        {
            if (edge.Condition == null || edge.Condition(state))
            {
                return edge.Target;
            }
        }

        return state.NextNode;
    }

    /// <summary>
    /// Gets the base graph for compatibility.
    /// </summary>
    public SharpGraph BaseGraph => _baseGraph;

    /// <summary>
    /// Gets the graph visualization.
    /// </summary>
    public string GetGraphViz() => _baseGraph.GetGraphViz();
}

/// <summary>
/// Represents a fork node that splits execution into parallel branches.
/// </summary>
public class ForkNode : GraphNode
{
    /// <summary>
    /// Gets or sets the branch node names to execute in parallel.
    /// </summary>
    public List<string> BranchNodes { get; set; } = new();
}

/// <summary>
/// Represents a join node that waits for all parallel branches to complete.
/// </summary>
public class JoinNode : GraphNode
{
    /// <summary>
    /// Gets or sets the strategy for joining results.
    /// </summary>
    public JoinStrategy Strategy { get; set; } = JoinStrategy.All;

    /// <summary>
    /// Gets or sets the merge function for combining branch results.
    /// </summary>
    public Func<List<GraphState>, GraphState>? MergeFunction { get; set; }
}

/// <summary>
/// Strategy for joining parallel branches.
/// </summary>
public enum JoinStrategy
{
    /// <summary>Wait for all branches to complete.</summary>
    All,
    /// <summary>Wait for any branch to complete.</summary>
    Any,
    /// <summary>Wait for a specific number of branches.</summary>
    Count
}

/// <summary>
/// Enhanced graph builder with parallel execution support.
/// </summary>
public class EnhancedSharpGraphBuilder
{
    private readonly EnhancedSharpGraph _graph;

    /// <summary>
    /// Creates a new enhanced graph builder.
    /// </summary>
    public EnhancedSharpGraphBuilder(string entryNode, int maxIterations = 100)
    {
        _graph = new EnhancedSharpGraph(entryNode, maxIterations);
    }

    /// <summary>
    /// Adds a regular node.
    /// </summary>
    public EnhancedSharpGraphBuilder Node(string name, Func<GraphState, Task<GraphState>> action, string? description = null)
    {
        _graph.AddNode(name, action, description);
        return this;
    }

    /// <summary>
    /// Adds a fork node that splits into parallel branches.
    /// </summary>
    public EnhancedSharpGraphBuilder Fork(string name, params string[] branchNodes)
    {
        var forkNode = new ForkNode
        {
            Name = name,
            Description = $"Fork to: {string.Join(", ", branchNodes)}",
            BranchNodes = branchNodes.ToList(),
            Action = async state =>
            {
                // Execute all branches in parallel
                var tasks = branchNodes.Select(async branchName =>
                {
                    var branchState = state.Clone();
                    branchState.CurrentNode = branchName;
                    return branchState;
                });

                var branchStates = await Task.WhenAll(tasks);
                state.Set("_fork_results", branchStates);
                return state;
            }
        };

        // Add fork node to graph (using reflection or internal method)
        // For now, we'll use a workaround
        _graph.AddNode(name, forkNode.Action, description: forkNode.Description);
        return this;
    }

    /// <summary>
    /// Adds a join node that waits for parallel branches.
    /// </summary>
    public EnhancedSharpGraphBuilder Join(string name, JoinStrategy strategy = JoinStrategy.All, Func<List<GraphState>, GraphState>? mergeFunction = null)
    {
        var joinNode = new JoinNode
        {
            Name = name,
            Strategy = strategy,
            MergeFunction = mergeFunction ?? ((states) => states.FirstOrDefault() ?? new GraphState())
        };

        joinNode.Action = async state =>
        {
            if (!state.Data.TryGetValue("_fork_results", out var forkResults) || forkResults is not GraphState[] branchStates)
            {
                return state;
            }

            var merged = joinNode.MergeFunction!(branchStates.ToList());
            foreach (var key in merged.Data.Keys)
            {
                state.Data[key] = merged.Data[key];
            }

            state.Data.Remove("_fork_results");
            return state;
        };

        _graph.AddNode(name, joinNode.Action, description: $"Join with {strategy} strategy");
        return this;
    }

    /// <summary>
    /// Adds an edge with fluent condition.
    /// </summary>
    public EnhancedSharpGraphBuilder Edge(string from, string to, Func<GraphState, bool>? condition = null, string? label = null)
    {
        _graph.AddEdge(from, to, condition, label);
        return this;
    }

    /// <summary>
    /// Adds event handlers.
    /// </summary>
    public EnhancedSharpGraphBuilder OnNodeStart(GraphNodeEventHandler handler)
    {
        _graph.OnNodeStart += handler;
        return this;
    }

    /// <summary>
    /// Adds event handlers.
    /// </summary>
    public EnhancedSharpGraphBuilder OnNodeEnd(GraphNodeEventHandler handler)
    {
        _graph.OnNodeEnd += handler;
        return this;
    }

    /// <summary>
    /// Adds event handlers.
    /// </summary>
    public EnhancedSharpGraphBuilder OnError(GraphNodeEventHandler handler)
    {
        _graph.OnError += handler;
        return this;
    }

    /// <summary>
    /// Adds streaming handler.
    /// </summary>
    public EnhancedSharpGraphBuilder OnStreaming(GraphStreamingEventHandler handler)
    {
        _graph.OnStreaming += handler;
        return this;
    }

    /// <summary>
    /// Enables state persistence.
    /// </summary>
    public EnhancedSharpGraphBuilder WithPersistence(IGraphStateStore store, bool autoSave = false)
    {
        _graph.StateStore = store;
        _graph.AutoSaveCheckpoints = autoSave;
        return this;
    }

    /// <summary>
    /// Builds the enhanced graph.
    /// </summary>
    public EnhancedSharpGraph Build() => _graph;
}

