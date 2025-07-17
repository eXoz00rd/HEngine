using HEngine.Core.Primitives;

namespace HEngine.Core.Contracts;

/// <summary>
///     Non-generic interface for component storage operations
///     Allows ComponentManager to work with storages without knowing the component type
/// </summary>
internal interface IComponentStorage {
    /// <summary>
    ///     Gets the number of components stored
    /// </summary>
    uint Count { get; }

    /// <summary>
    ///     Gets the type of component this storage handles
    /// </summary>
    Type ComponentType { get; }

    /// <summary>
    ///     Removes all components associated with the specified entity
    /// </summary>
    /// <param name="entity">The entity to remove components for</param>
    /// <returns>True if any component was removed, false otherwise</returns>
    bool RemoveEntity(Entity entity);

    /// <summary>
    ///     Checks if the storage contains a component for the specified entity
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True if the entity has a component in this storage</returns>
    bool HasEntity(Entity entity);

    /// <summary>
    ///     Gets all entities that have components in this storage
    /// </summary>
    /// <param name="entities">List to populate with entities</param>
    void GetAllEntities(List<Entity> entities);

    /// <summary>
    ///     Clears all components from the storage
    /// </summary>
    void Clear();

    /// <summary>
    ///     Compacts the storage by removing gaps left by deleted components
    ///     This can improve cache performance but invalidates any existing references
    /// </summary>
    void Compact();

    /// <summary>
    ///     Gets memory usage statistics for this storage
    /// </summary>
    /// <returns>Memory usage in bytes</returns>
    long GetMemoryUsage();
}

/// <summary>
///     Generic interface for type-safe component storage operations
/// </summary>
/// <typeparam name="T">The component type</typeparam>
internal interface IComponentStorage<T> : IComponentStorage where T : struct, IComponent {
    /// <summary>
    ///     Adds a component to the specified entity
    /// </summary>
    /// <param name="entity">The entity to add the component to</param>
    /// <param name="component">The component data</param>
    /// <returns>Reference to the stored component</returns>
    ref T AddComponent(Entity entity, in T component);

    /// <summary>
    ///     Removes the component from the specified entity
    /// </summary>
    /// <param name="entity">The entity to remove the component from</param>
    /// <returns>True if the component was removed, false if it didn't exist</returns>
    bool RemoveComponent(Entity entity);

    /// <summary>
    ///     Checks if the entity has this component type
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>True if the entity has the component</returns>
    bool HasComponent(Entity entity);

    /// <summary>
    ///     Gets a reference to the component for the specified entity
    /// </summary>
    /// <param name="entity">The entity to get the component for</param>
    /// <returns>Reference to the component</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity doesn't have the component</exception>
    ref T GetComponent(Entity entity);

    /// <summary>
    ///     Tries to get the component for the specified entity
    /// </summary>
    /// <param name="entity">The entity to get the component for</param>
    /// <param name="component">The component data if found</param>
    /// <returns>True if the component was found, false otherwise</returns>
    bool TryGetComponent(Entity entity, out T component);

    /// <summary>
    ///     Gets all components of this type as a span
    ///     Note: The span is only valid until the next modification to the storage
    /// </summary>
    /// <returns>Span containing all active components</returns>
    Span<T> GetAllComponents();

    /// <summary>
    ///     Gets all entities that have this component type
    /// </summary>
    /// <param name="entities">List to populate with entities</param>
    void GetEntitiesWithComponent(List<Entity> entities);
}