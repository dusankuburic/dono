using Microsoft.JSInterop;

namespace BuildingManager.Blazor.Services;

public static class FileSaveExtensions
{
    public static Task SaveAsync(this IJSRuntime js, DownloadedDocument file) =>
        js.InvokeVoidAsync("saveFile", file.FileName, file.ContentType,
            Convert.ToBase64String(file.Content)).AsTask();
}
