using VetMS.Data;
using VetMS.Models;

namespace VetMS.Services;

public class AppStateService
{
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public string PageTitle { get; set; } = "Dashboard";

    public event Action? OnChange;

    public bool Login(string username, string password)
    {
        var user = Database.AuthenticateUser(username, password);
        if (user == null) return false;
        CurrentUser = user;
        AppSession.SetUser(user);
        NotifyStateChanged();
        return true;
    }

    public void Logout()
    {
        CurrentUser = null;
        AppSession.Clear();
        NotifyStateChanged();
    }

    public void UpdateCurrentUser(User user)
    {
        CurrentUser = user;
        AppSession.SetUser(user);
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
