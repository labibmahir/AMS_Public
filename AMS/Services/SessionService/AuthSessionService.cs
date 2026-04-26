using System.Text.Json;
using Microsoft.JSInterop;
using AMS.Domains.Data;

namespace AMS.Services.SessionService;

// Stores auth session in browser localStorage so it survives SSR navigations + refresh.
public sealed class AuthSessionService
{
    private const string StorageKey = "ams.session";
    private readonly IJSRuntime js;

    private UserSession cached;

    public AuthSessionService(IJSRuntime js)
    {
        this.js = js;
    }

    public sealed record UserSession(string UserId, string FullName, string Username, Enums.UserType UserType, long ExpiresAtUtcMs);

    public async Task<UserSession> GetSessionAsync()
    {
        if (cached is not null && !IsExpired(cached))
        {
            return cached;
        }

        string json;
        try
        {
            json = await js.InvokeAsync<string>("localStorage.getItem", StorageKey);
        }
        catch
        {
            // JS interop unavailable (e.g., during prerender).
            return null;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            cached = null;
            return null;
        }

        UserSession session;
        try
        {
            session = JsonSerializer.Deserialize<UserSession>(json);
        }
        catch
        {
            await ClearSessionAsync();
            return null;
        }

        if (session is null || IsExpired(session))
        {
            await ClearSessionAsync();
            return null;
        }

        cached = session;
        return session;
    }

    public async Task SetSessionAsync(UserSession session)
    {
        cached = session;
        var json = JsonSerializer.Serialize(session);
        await js.InvokeAsync<object>("localStorage.setItem", StorageKey, json);
    }

    public async Task ClearSessionAsync()
    {
        cached = null;
        try
        {
            await js.InvokeAsync<object>("localStorage.removeItem", StorageKey);
        }
        catch
        {
            // ignore
        }
    }

    public static UserSession Create(string userId, string fullName, string username, Enums.UserType userType)
    {
        var expires = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeMilliseconds();
        return new UserSession(userId, fullName, username, userType, expires);
    }

    private static bool IsExpired(UserSession session)
        => session.ExpiresAtUtcMs <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
