namespace VetMS.Services;

public record ToastMessage(string Message, string Type);

public class ToastService
{
    public List<ToastMessage> Toasts { get; } = [];
    public event Action? OnChange;

    public void Show(string message, string type = "info")
    {
        var toast = new ToastMessage(message, type);
        Toasts.Add(toast);
        OnChange?.Invoke();
        _ = RemoveAfterDelay(toast);
    }

    public void Success(string message) => Show(message, "success");
    public void Error(string message) => Show(message, "error");
    public void Info(string message) => Show(message, "info");

    private async Task RemoveAfterDelay(ToastMessage toast)
    {
        await Task.Delay(3000);
        Toasts.Remove(toast);
        OnChange?.Invoke();
    }
}
