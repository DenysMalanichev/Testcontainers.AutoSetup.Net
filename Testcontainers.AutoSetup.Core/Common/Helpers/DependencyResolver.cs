namespace Testcontainers.AutoSetup.Core.Common.Helpers;

internal class DependencyResolver
{
    private readonly Dictionary<Type, object> _services = [];

    /// <summary>
    /// Register an instance of a service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance"></param>
    public void Register<T>(T instance) where T : notnull
    {
        _services[typeof(T)] = instance;
    }

    /// <summary>
    /// Create an instance of the specified type, resolving its dependencies
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public object CreateInstance(Type type)
    {
        var constructor = type.GetConstructors().FirstOrDefault() 
                          ?? throw new InvalidOperationException($"No public constructor found for {type.Name}");

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            // Check if we have this dependency registered
            if (_services.TryGetValue(paramType, out var service))
            {
                args[i] = service;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unable to resolve service for type '{paramType.Name}' while constructing '{type.Name}'.");
            }
        }

        return constructor.Invoke(args);
    }

    /// <summary>
    /// Create an instance of the specified type, resolving its dependencies
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T CreateInstance<T>() => (T)CreateInstance(typeof(T));
}