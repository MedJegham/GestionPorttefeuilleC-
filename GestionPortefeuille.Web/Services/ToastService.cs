namespace GestionPortefeuille.Web.Services;

public enum ToastLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public ToastLevel Level { get; init; } = ToastLevel.Info;
}

public class ToastService
{
    private readonly List<ToastMessage> _messages = new();
    public IReadOnlyList<ToastMessage> Messages => _messages;

    public event Action? OnChange;

    public void ShowInfo(string title, string body = "") => Push(new ToastMessage { Title = title, Body = body, Level = ToastLevel.Info });
    public void ShowSuccess(string title, string body = "") => Push(new ToastMessage { Title = title, Body = body, Level = ToastLevel.Success });
    public void ShowWarning(string title, string body = "") => Push(new ToastMessage { Title = title, Body = body, Level = ToastLevel.Warning });
    public void ShowError(string title, string body = "") => Push(new ToastMessage { Title = title, Body = body, Level = ToastLevel.Error });

    public void Dismiss(Guid id)
    {
        var removed = _messages.RemoveAll(m => m.Id == id);
        if (removed > 0) OnChange?.Invoke();
    }

    private void Push(ToastMessage msg)
    {
        _messages.Add(msg);
        OnChange?.Invoke();

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            Dismiss(msg.Id);
        });
    }
}
