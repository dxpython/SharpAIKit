using System.Text.Json;
using SharpAIKit.Common;

namespace SharpAIKit.Graph;

/// <summary>
/// Represents a checkpoint for graph state persistence.
/// </summary>
public class GraphCheckpoint
{
    /// <summary>
    /// Gets or sets the checkpoint ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the graph name.
    /// </summary>
    public string GraphName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current node.
    /// </summary>
    public string CurrentNode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the graph state data.
    /// </summary>
    public Dictionary<string, object?> StateData { get; set; } = new();

    /// <summary>
    /// Gets or sets the execution history.
    /// </summary>
    public List<string> ExecutionHistory { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for graph state persistence.
/// </summary>
public interface IGraphStateStore
{
    /// <summary>
    /// Saves a checkpoint.
    /// </summary>
    Task SaveCheckpointAsync(GraphCheckpoint checkpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a checkpoint by ID.
    /// </summary>
    Task<GraphCheckpoint?> LoadCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all checkpoints for a graph.
    /// </summary>
    Task<List<GraphCheckpoint>> ListCheckpointsAsync(string graphName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a checkpoint.
    /// </summary>
    Task DeleteCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of graph state store.
/// </summary>
public class MemoryGraphStateStore : IGraphStateStore
{
    private readonly Dictionary<string, GraphCheckpoint> _checkpoints = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public Task SaveCheckpointAsync(GraphCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            checkpoint.UpdatedAt = DateTime.UtcNow;
            _checkpoints[checkpoint.Id] = checkpoint;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<GraphCheckpoint?> LoadCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _checkpoints.TryGetValue(checkpointId, out var checkpoint);
            return Task.FromResult(checkpoint);
        }
    }

    /// <inheritdoc/>
    public Task<List<GraphCheckpoint>> ListCheckpointsAsync(string graphName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_checkpoints.Values
                .Where(c => c.GraphName == graphName)
                .OrderByDescending(c => c.UpdatedAt)
                .ToList());
        }
    }

    /// <inheritdoc/>
    public Task DeleteCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _checkpoints.Remove(checkpointId);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// File-based implementation of graph state store.
/// </summary>
public class FileGraphStateStore : IGraphStateStore
{
    private readonly string _directory;

    /// <summary>
    /// Creates a new file-based state store.
    /// </summary>
    public FileGraphStateStore(string directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        Directory.CreateDirectory(_directory);
    }

    private string GetFilePath(string checkpointId) => Path.Combine(_directory, $"{checkpointId}.json");

    /// <inheritdoc/>
    public async Task SaveCheckpointAsync(GraphCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        checkpoint.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions { WriteIndented = true });
        var filePath = GetFilePath(checkpoint.Id);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GraphCheckpoint?> LoadCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(checkpointId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<GraphCheckpoint>(json);
    }

    /// <inheritdoc/>
    public Task<List<GraphCheckpoint>> ListCheckpointsAsync(string graphName, CancellationToken cancellationToken = default)
    {
        var checkpoints = new List<GraphCheckpoint>();
        var files = Directory.GetFiles(_directory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var checkpoint = JsonSerializer.Deserialize<GraphCheckpoint>(json);
                if (checkpoint != null && checkpoint.GraphName == graphName)
                {
                    checkpoints.Add(checkpoint);
                }
            }
            catch
            {
                // Skip invalid files
            }
        }

        return Task.FromResult(checkpoints.OrderByDescending(c => c.UpdatedAt).ToList());
    }

    /// <inheritdoc/>
    public Task DeleteCheckpointAsync(string checkpointId, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(checkpointId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for graph state persistence.
/// </summary>
public static class GraphPersistenceExtensions
{
    /// <summary>
    /// Saves the current graph state as a checkpoint.
    /// </summary>
    public static async Task<string> SaveCheckpointAsync(this SharpGraph graph, GraphState state, IGraphStateStore store, string? checkpointId = null)
    {
        var checkpoint = new GraphCheckpoint
        {
            Id = checkpointId ?? Guid.NewGuid().ToString(),
            GraphName = graph.GetType().Name,
            CurrentNode = state.CurrentNode,
            StateData = state.Data,
            ExecutionHistory = new List<string> { state.CurrentNode }
        };

        await store.SaveCheckpointAsync(checkpoint);
        return checkpoint.Id;
    }

    /// <summary>
    /// Restores graph state from a checkpoint.
    /// </summary>
    public static async Task<GraphState?> RestoreFromCheckpointAsync(this SharpGraph graph, string checkpointId, IGraphStateStore store)
    {
        var checkpoint = await store.LoadCheckpointAsync(checkpointId);
        if (checkpoint == null)
        {
            return null;
        }

        return new GraphState
        {
            CurrentNode = checkpoint.CurrentNode,
            Data = checkpoint.StateData
        };
    }
}

