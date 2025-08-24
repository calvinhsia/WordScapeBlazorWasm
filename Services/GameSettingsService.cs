using Microsoft.JSInterop;
using System.Text.Json;
using WordScapeBlazorWasm.Models;

namespace WordScapeBlazorWasm.Services
{
    public class GameSettingsService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string SETTINGS_KEY = "wordscape_settings";

        public GameSettingsService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<GameSettings> LoadSettingsAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", SETTINGS_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
                }
            }
            catch
            {
                // If there's an error loading settings, return default
            }
            return new GameSettings();
        }

        public async Task SaveSettingsAsync(GameSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SETTINGS_KEY, json);
            }
            catch
            {
                // Handle error silently - settings just won't persist
            }
        }
    }
}
