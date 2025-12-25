namespace SharpAIKit.Graph;

/// <summary>
/// Fluent API builder for creating graphs with a more elegant syntax.
/// </summary>
public class FluentGraphBuilder
{
    private readonly EnhancedSharpGraphBuilder _builder;
    private GraphNode? _currentNode;

    /// <summary>
    /// Creates a new fluent graph builder.
    /// </summary>
    public FluentGraphBuilder(string entryNode, int maxIterations = 100)
    {
        _builder = new EnhancedSharpGraphBuilder(entryNode, maxIterations);
    }

    /// <summary>
    /// Starts a new node.
    /// </summary>
    public FluentNode Step(string name)
    {
        _currentNode = new GraphNode { Name = name };
        return new FluentNode(this, name);
    }

    /// <summary>
    /// Adds the current node to the graph.
    /// </summary>
    internal void AddCurrentNode(Func<GraphState, Task<GraphState>> action)
    {
        if (_currentNode != null)
        {
            _builder.Node(_currentNode.Name, action, _currentNode.Description);
            _currentNode = null;
        }
    }

    /// <summary>
    /// Builds the graph.
    /// </summary>
    public EnhancedSharpGraph Build() => _builder.Build();

    /// <summary>
    /// Gets the underlying builder for advanced operations.
    /// </summary>
    public EnhancedSharpGraphBuilder GetBuilder() => _builder;
}

/// <summary>
/// Fluent node builder for chain-style API.
/// </summary>
public class FluentNode
{
    private readonly FluentGraphBuilder _builder;
    private readonly string _nodeName;
    private Func<GraphState, Task<GraphState>>? _action;
    private string? _description;

    internal FluentNode(FluentGraphBuilder builder, string nodeName)
    {
        _builder = builder;
        _nodeName = nodeName;
    }

    /// <summary>
    /// Sets the node action.
    /// </summary>
    public FluentNode Do(Func<GraphState, Task<GraphState>> action)
    {
        _action = action;
        return this;
    }

    /// <summary>
    /// Sets the node description.
    /// </summary>
    public FluentNode Describe(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Transitions to the next node.
    /// </summary>
    public FluentNode Next(string nextNodeName)
    {
        if (_action == null)
        {
            throw new InvalidOperationException("Node action must be set before calling Next()");
        }

        _builder.AddCurrentNode(_action);
        _builder.GetBuilder().Edge(_nodeName, nextNodeName);
        return _builder.Step(nextNodeName);
    }

    /// <summary>
    /// Transitions to the next node conditionally.
    /// </summary>
    public FluentNode Next(string nextNodeName, Func<GraphState, bool> condition)
    {
        if (_action == null)
        {
            throw new InvalidOperationException("Node action must be set before calling Next()");
        }

        _builder.AddCurrentNode(_action);
        _builder.GetBuilder().Edge(_nodeName, nextNodeName, condition);
        return _builder.Step(nextNodeName);
    }

    /// <summary>
    /// Adds a conditional branch.
    /// </summary>
    public FluentNode If(Func<GraphState, bool> condition, string trueNode, string? falseNode = null)
    {
        if (_action == null)
        {
            throw new InvalidOperationException("Node action must be set before calling If()");
        }

        _builder.AddCurrentNode(_action);
        _builder.GetBuilder().Edge(_nodeName, trueNode, condition);
        
        if (!string.IsNullOrEmpty(falseNode))
        {
            _builder.GetBuilder().Edge(_nodeName, falseNode, state => !condition(state));
        }

        return this;
    }

    /// <summary>
    /// Ends the graph execution.
    /// </summary>
    public FluentGraphBuilder End()
    {
        if (_action == null)
        {
            throw new InvalidOperationException("Node action must be set before calling End()");
        }

        _builder.AddCurrentNode(async state =>
        {
            state = await _action(state);
            state.ShouldEnd = true;
            return state;
        });

        return _builder;
    }
}

/// <summary>
/// Extension methods for fluent graph building.
/// </summary>
public static class FluentGraphExtensions
{
    /// <summary>
    /// Creates a fluent graph builder.
    /// </summary>
    public static FluentNode StartGraph(string entryNode, int maxIterations = 100)
    {
        var builder = new FluentGraphBuilder(entryNode, maxIterations);
        return builder.Step(entryNode);
    }
}

