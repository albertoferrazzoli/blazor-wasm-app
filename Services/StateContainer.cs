using System.Linq.Expressions;
using System.Reflection;

namespace blazor_wasm_app.Services;

public class StateContainer<T> where T : class, new()
{
    private T _state = new();
    private readonly List<Action> _subscribers = new();
    private readonly Dictionary<string, List<Action<object?>>> _propertySubscribers = new();

    public T State => _state;

    public void SetState(T newState)
    {
        _state = newState;
        NotifyAll();
    }

    public void UpdateState(Action<T> updateAction)
    {
        updateAction(_state);
        NotifyAll();
    }

    public void SetProperty<TValue>(Expression<Func<T, TValue>> propertySelector, TValue value)
    {
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access");

        var propertyInfo = memberExpression.Member as PropertyInfo
            ?? throw new ArgumentException("Expression must be a property");

        propertyInfo.SetValue(_state, value);

        NotifyPropertySubscribers(propertyInfo.Name, value);
        NotifyAll();
    }

    public TValue GetProperty<TValue>(Expression<Func<T, TValue>> propertySelector)
    {
        var compiled = propertySelector.Compile();
        return compiled(_state);
    }

    public void Subscribe(Action callback)
    {
        _subscribers.Add(callback);
    }

    public void SubscribeToProperty<TValue>(Expression<Func<T, TValue>> propertySelector, Action<TValue?> callback)
    {
        var memberExpression = propertySelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property access");

        var propertyName = memberExpression.Member.Name;

        if (!_propertySubscribers.ContainsKey(propertyName))
        {
            _propertySubscribers[propertyName] = new List<Action<object?>>();
        }

        _propertySubscribers[propertyName].Add(obj => callback((TValue?)obj));
    }

    public void Unsubscribe(Action callback)
    {
        _subscribers.Remove(callback);
    }

    private void NotifyAll()
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber();
        }
    }

    private void NotifyPropertySubscribers(string propertyName, object? value)
    {
        if (_propertySubscribers.TryGetValue(propertyName, out var callbacks))
        {
            foreach (var callback in callbacks)
            {
                callback(value);
            }
        }
    }
}

public class AppState
{
    public string? CurrentUser { get; set; }
    public bool IsAuthenticated { get; set; }
    public string Theme { get; set; } = "light";
    public List<string> Notifications { get; set; } = new();
    public Dictionary<string, object> UserPreferences { get; set; } = new();
}

public class AppStateService : StateContainer<AppState>
{
    public void Login(string username)
    {
        UpdateState(state =>
        {
            state.CurrentUser = username;
            state.IsAuthenticated = true;
            state.Notifications.Add($"Welcome, {username}!");
        });
    }

    public void Logout()
    {
        UpdateState(state =>
        {
            state.CurrentUser = null;
            state.IsAuthenticated = false;
            state.Notifications.Clear();
        });
    }

    public void ToggleTheme()
    {
        SetProperty(s => s.Theme, State.Theme == "light" ? "dark" : "light");
    }

    public void AddNotification(string message)
    {
        UpdateState(state => state.Notifications.Add(message));
    }
}
