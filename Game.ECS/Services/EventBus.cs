namespace Game.ECS.Services;

public interface IEvent { }

public sealed class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> _listeners = new();

    public void Subscribe<T>(Action<T> listener) where T : IEvent
    {
        if (!_listeners.TryGetValue(typeof(T), out var list))
            _listeners[typeof(T)] = list = new List<Delegate>();
        list.Add(listener);
    }

    public void Unsubscribe<T>(Action<T> listener) where T : IEvent
    {
        if (_listeners.TryGetValue(typeof(T), out var list))
            list.Remove(listener);
    }

    public void Publish<T>(T ev) where T : IEvent
    {
        if (_listeners.TryGetValue(typeof(T), out var list))
            foreach (var listener in list.Cast<Action<T>>())
                listener(ev);
    }
}