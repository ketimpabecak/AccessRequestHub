using Microsoft.JSInterop;
using System.Text.Json;

namespace AccessRequestHub.Client.Services;

public class UserState
{
    private readonly IJSRuntime _js;
    public string CurrentUserId { get; private set; } = "11111111-1111-1111-1111-111111111111"; // Default Alice
    public string CurrentUserName { get; private set; } = "Alice";

    public static readonly Dictionary<string, string> Users = new()
    {
        { "11111111-1111-1111-1111-111111111111", "Alice (Requester)" },
        { "22222222-2222-2222-2222-222222222222", "Bob (Manager)" },
        { "33333333-3333-3333-3333-333333333333", "Carol (System Owner CRM)" },
        { "44444444-4444-4444-4444-444444444444", "Dana (System Owner Finance)" },
        { "55555555-5555-5555-5555-555555555555", "Erin (Admin)" }
    };

    public UserState(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetUserAsync(string userId)
    {
        CurrentUserId = userId;
        CurrentUserName = Users.TryGetValue(userId, out var name) ? name : "Unknown";

        await _js.InvokeVoidAsync("localStorage.setItem", "currentUserId", userId);
        OnUserChanged?.Invoke();
    }

    public async Task LoadUserAsync()
    {
        var savedId = await _js.InvokeAsync<string>("localStorage.getItem", "currentUserId");
        if (!string.IsNullOrEmpty(savedId) && Users.ContainsKey(savedId))
        {
            CurrentUserId = savedId;
            CurrentUserName = Users[savedId];
        }
    }

    public event Action? OnUserChanged;
}