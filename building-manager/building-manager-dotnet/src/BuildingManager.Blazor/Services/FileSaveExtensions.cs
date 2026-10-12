using Microsoft.JSInterop;

namespace BuildingManager.Blazor.Services;

/// <summary>
/// Saves an API download to a file. The API is JWT-protected, so a plain
/// link would not carry the Authorization header — the bytes go through
/// Blazor and are handed to the saveFile helper in index.html.
/// </summary>
public static class FileSaveExtensions
{
    public static Task SaveAsync(this IJSRuntime js, DownloadedDocument file) =>
        js.InvokeVoidAsync("saveFile", file.FileName, file.ContentType,
            Convert.ToBase64String(file.Content)).AsTask();
}
