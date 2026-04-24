using VetMS.Models;

namespace VetMS.Data;

/// <summary>
/// Holds the currently authenticated user for the session.
/// Set once at login; used throughout the app to stamp created_by / updated_by.
/// </summary>
public static class AppSession
{
    public static User? CurrentUser { get; private set; }

    public static string Username => CurrentUser?.Username ?? "System";

    public static void SetUser(User user) => CurrentUser = user;

    public static void Clear() => CurrentUser = null;
}
