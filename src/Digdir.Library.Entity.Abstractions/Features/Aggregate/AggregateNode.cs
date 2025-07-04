using System.Reflection;
using Digdir.Library.Entity.Abstractions.Features.Updatable;

namespace Digdir.Library.Entity.Abstractions.Features.Aggregate;

/// <summary>
/// Represents a node in an aggregate tree.
/// </summary>
public abstract class AggregateNode
{
    private static readonly Type OpenGenericAggregateNodeType = typeof(AggregateNode<>);
    private readonly HashSet<AggregateNode> _children = [];
    private readonly HashSet<AggregateNode> _parents = [];
    private readonly List<AggregateNodeProperty> _modifiedProperties;

    /// <summary>
    /// The actual entity in the aggregate tree this node represents.
    /// </summary>
    public object Entity { get; }

    /// <summary>
    /// A collection of modified properties on this aggregate node.
    /// </summary>
    public IReadOnlyCollection<AggregateNodeProperty> ModifiedProperties => _modifiedProperties;

    /// <summary>
    /// A collection of modified children. A child node is modified if it itself
    /// is modified, or one of its children are modified. Modified in this
    /// context means added, modified, or deleted.
    /// </summary>
    public IReadOnlyCollection<AggregateNode> Children => _children;

    /// <summary>
    /// A collection of parents.
    /// </summary>
    public IReadOnlyCollection<AggregateNode> Parents => _parents;

    /// <summary>
    /// The state of the <see cref="Entity"/> this node represents.
    /// </summary>
    public AggregateNodeState State { get; internal set; }

    /// <summary>
    /// True when the <see cref="Entity"/> this node represents has been deleted by a parent node. False if directly deleted.
    /// </summary>
    public bool DeletedByParent { get; internal set; }

    /// <summary>
    /// True when the <see cref="Entity"/> this node represents has been modified by a child node. False if directly modified.
    /// </summary>
    public bool ModifiedByChild { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateNode"/> class.
    /// </summary>
    protected AggregateNode(
        object entity,
        AggregateNodeState state,
        IEnumerable<AggregateNodeProperty> modifiedProperties)
    {
        Entity = entity;
        State = state;
        _modifiedProperties = modifiedProperties.ToList();
    }

    internal void AddChild(AggregateNode node) => _children.Add(node);
    internal void AddParent(AggregateNode node) => _parents.Add(node);

    internal bool IsLeafNode => _children.Count == 0;
    internal bool IsRootNode => _parents.Count == 0;

    internal static AggregateNode Create(Type type, object entity, AggregateNodeState state,
        IEnumerable<AggregateNodeProperty> modifiedProperties)
    {
        if (!entity.GetType().IsAssignableTo(type))
        {
            throw new ArgumentException(
                $"Parameter {nameof(entity)} ({entity.GetType()}) must be assignable to {type}.");
        }

        var nodeArguments = new[] { entity, state, modifiedProperties };
        var genericType = OpenGenericAggregateNodeType.MakeGenericType(type);
        var node = (AggregateNode)Activator.CreateInstance(genericType, BindingFlags.NonPublic | BindingFlags.Instance,
            null, nodeArguments, null)!;
        return node;
    }

    /// <summary>
    /// Convenience method to check if the state of the node is <see cref="AggregateNodeState.Modified"/> and not by a child node.
    /// </summary>
    public bool IsDirectlyModified() => State is AggregateNodeState.Modified && !ModifiedByChild;

    /// <summary>
    /// Convenience method to check if the state of the node is <see cref="AggregateNodeState.Added"/>.
    /// </summary>
    public bool IsAdded() => State is AggregateNodeState.Added;

    /// <summary>
    /// Convenience method to check if the state of the node is <see cref="AggregateNodeState.Modified"/>.
    /// </summary>
    public bool IsModified() => State is AggregateNodeState.Modified;

    /// <summary>
    /// Convenience method to check if the state of the node is <see cref="AggregateNodeState.Restored"/>.
    /// </summary>
    public bool IsRestored() => State is AggregateNodeState.Restored;

    /// <summary>
    /// Convenience method to check if the state of the node is <see cref="AggregateNodeState.Deleted"/>.
    /// </summary>
    public bool IsDeleted() => State is AggregateNodeState.Deleted;

    /// <summary>
    /// Convenience method to check if the state of the node is <see cref="AggregateNodeState.Added"/>,
    /// and that UpdatedAt is the default value.
    /// </summary>
    /// <param name="updatable"></param>
    public bool IsAddedWithDefaultUpdatedAt(IUpdateableEntity updatable)
        => IsAdded() && updatable.UpdatedAt == default;
}

/// <summary>
/// Represents a type specific node in an aggregate tree.
/// </summary>
/// <typeparam name="T">The type this node represents.</typeparam>
public sealed class AggregateNode<T> : AggregateNode
    where T : notnull
{
    /// <summary>
    /// The actual entity in the aggregate tree this node represents.
    /// </summary>
    public new T Entity => (T)base.Entity;

    private AggregateNode(T entity, AggregateNodeState state, IEnumerable<AggregateNodeProperty> modifiedProperties)
    : base(entity, state, modifiedProperties)
    {
    }
}

/// <summary>
/// The entity state in which a <see cref="AggregateNode"/> represents.
/// </summary>
public enum AggregateNodeState
{
    /// <summary>
    /// The entity has been added, but does not yet exist in the database.
    /// </summary>
    Added = 1,

    /// <summary>
    /// All or some of the entities property values, or part of its aggregate children chain have been modified.
    /// </summary>
    Modified = 2,

    /// <summary>
    /// The entity, or part of its aggregate parent chain have been marked for deletion.
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// The entities property values have not been changed from the values in the database.
    /// </summary>
    Unchanged = 4,

    /// <summary>
    /// The entity, or its aggregate parent chain have been marked for restoration.
    /// </summary>
    Restored = 5
}

/// <summary>
/// Represents a property of an <see cref="AggregateNode"/> that has been modified.
/// </summary>
public abstract class AggregateNodeProperty
{
    private static readonly Type OpenGenericAggregateNodePropertyType = typeof(AggregateNodeProperty<>);

    /// <summary>
    /// Gets the name of the name of the property that has been modified.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the original value of the property before it was modified.
    /// </summary>
    public object? OriginalValue { get; }

    /// <summary>
    /// Gets the current value of the property after it was modified.
    /// </summary>
    public object? CurrentValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateNodeProperty"/> class.
    /// </summary>
    /// <param name="property">The name of the property that has been modified.</param>
    /// <param name="originalValue">The original value of the property before modification.</param>
    /// <param name="currentValue">The current value of the property after modification.</param>
    protected AggregateNodeProperty(string property, object? originalValue, object? currentValue)
    {
        PropertyName = property;
        OriginalValue = originalValue;
        CurrentValue = currentValue;
    }

    internal static AggregateNodeProperty Create(
        Type propertyType,
        string propertyName,
        object? originalValue,
        object? currentValue)
    {
        var nodeArguments = new[] { propertyName, originalValue, currentValue };
        var genericType = OpenGenericAggregateNodePropertyType.MakeGenericType(propertyType);
        var property = (AggregateNodeProperty)Activator.CreateInstance(genericType,
            BindingFlags.NonPublic | BindingFlags.Instance, null, nodeArguments, null)!;
        return property;
    }
}

/// <summary>
/// Represents a strongly-typed property of an <see cref="AggregateNode{T}"/> that has been modified.
/// </summary>
/// <typeparam name="T">The type of the property.</typeparam>
public sealed class AggregateNodeProperty<T> : AggregateNodeProperty
{
    /// <summary>
    /// Gets the original value of the property before it was modified.
    /// </summary>
    public new T? OriginalValue => (T?)base.OriginalValue;

    /// <summary>
    /// Gets the original value of the property before it was modified.
    /// </summary>
    public new T? CurrentValue => (T?)base.CurrentValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateNodeProperty{T}"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that has been modified.</param>
    /// <param name="originalValue">The original value of the property before modification.</param>
    /// <param name="currentValue">The current value of the property after modification.</param>
    private AggregateNodeProperty(string propertyName, T originalValue, T currentValue) : base(propertyName,
        originalValue, currentValue)
    {
    }
}
