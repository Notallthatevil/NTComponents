# NTInputFile Minimal Setups

This guide shows the simplest setup for each hosting model:

1. `InteractiveServer`
2. `InteractiveWebAssembly`
3. `SSR` (non-interactive)

## Shared Notes

- `NTInputFile` progress updates happen while your code reads `NTInputFileEventArgs.Stream`.
- Wire `OnUploadButtonClick` or the upload button will not render.
- `InteractiveServer` does not need SSE for progress.
- Use SSE only when a `WebAssembly` client needs server-side progress from API/background work.

## 1) InteractiveServer (no SSE)

Use this when upload processing runs directly in the server component.

```razor
@rendermode InteractiveServer
@using NTComponents
@using Microsoft.AspNetCore.Components.Forms

<NTInputFile Label="Upload file"
             MaximumFileCount="1"
             MaximumFileSize="52428800"
             ShowProgress="true"
             OnUploadButtonClick="OnUploadButtonClickAsync"
             OnUploadFile="OnUploadFileAsync"
             OnProgressChanged="OnProgressChangedAsync" />

<p>@_status (@_percent%)</p>

@code {
    private int _percent;
    private string _status = "Ready";

    private Task OnUploadButtonClickAsync(IReadOnlyList<IBrowserFile> _)
        => Task.CompletedTask;

    private async Task OnUploadFileAsync(NTInputFileEventArgs args) {
        if (args.Stream is null) {
            return;
        }

        var uploadPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
        Directory.CreateDirectory(uploadPath);
        var filePath = Path.Combine(uploadPath, Path.GetFileName(args.Name));

        await using var output = File.Create(filePath);
        await args.Stream.CopyToAsync(output);
        _status = "Complete";
    }

    private Task OnProgressChangedAsync(NTInputFileEventArgs args) {
        _percent = args.ProgressPercent;
        _status = args.ProgressTitle;
        return InvokeAsync(StateHasChanged);
    }
}
```

## 2) InteractiveWebAssembly

Use this when the component runs in the browser and uploads to an API.

```razor
@rendermode InteractiveWebAssembly
@using NTComponents
@using Microsoft.AspNetCore.Components.Forms
@using System.Net.Http.Headers
@inject HttpClient Http

<NTInputFile Label="Upload file"
             MaximumFileCount="1"
             MaximumFileSize="52428800"
             ShowProgress="true"
             OnUploadButtonClick="OnUploadButtonClickAsync"
             OnUploadFile="OnUploadFileAsync"
             OnProgressChanged="OnProgressChangedAsync" />

<p>@_status (@_percent%)</p>

@code {
    private int _percent;
    private string _status = "Ready";

    private Task OnUploadButtonClickAsync(IReadOnlyList<IBrowserFile> _)
        => Task.CompletedTask;

    private async Task OnUploadFileAsync(NTInputFileEventArgs args) {
        if (args.Stream is null) {
            return;
        }

        using var form = new MultipartFormDataContent();
        using var file = new StreamContent(args.Stream);
        file.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(args.ContentType) ? "application/octet-stream" : args.ContentType);

        form.Add(file, "file", args.Name);

        using var response = await Http.PostAsync("api/uploads/process", form);
        response.EnsureSuccessStatusCode();
        _status = "Complete";
    }

    private Task OnProgressChangedAsync(NTInputFileEventArgs args) {
        _percent = args.ProgressPercent;
        _status = args.ProgressTitle;
        return InvokeAsync(StateHasChanged);
    }
}
```

Minimal API endpoint:

```csharp
app.MapPost("/api/uploads/process", async (IFormFile file, CancellationToken ct) => {
    if (file.Length <= 0) {
        return Results.BadRequest("Empty file.");
    }

    var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
    Directory.CreateDirectory(root);
    var path = Path.Combine(root, Path.GetFileName(file.FileName));

    await using var output = File.Create(path);
    await file.CopyToAsync(output, ct);
    return Results.Ok();
}).DisableAntiforgery();
```

### InteractiveWebAssembly with SSE (server progress)

Use this when WebAssembly needs progress from server-side processing.
Add `@using System.Globalization` in the component using this pattern.

Client pattern:

```csharp
private async Task OnUploadFileAsync(NTInputFileEventArgs args) {
    if (args.Stream is null) {
        return;
    }

    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    var uploadId = Guid.NewGuid().ToString("N");

    var listenTask = ListenForProgressAsync(uploadId, timeout.Token);

    using var form = new MultipartFormDataContent();
    form.Add(new StringContent(uploadId), "uploadId");
    form.Add(new StringContent(args.Size.ToString(CultureInfo.InvariantCulture)), "fileSize");
    form.Add(new StreamContent(args.Stream), "file", args.Name);

    using var response = await Http.PostAsync("api/uploads/process", form, timeout.Token);
    response.EnsureSuccessStatusCode();

    await listenTask;
}
```

SSE listener shape:

```csharp
private async Task ListenForProgressAsync(string uploadId, CancellationToken ct) {
    using var req = new HttpRequestMessage(HttpMethod.Get, $"api/uploads/progress/{uploadId}/net9");
    req.Headers.Accept.ParseAdd("text/event-stream");

    using var response = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
    response.EnsureSuccessStatusCode();

    await using var stream = await response.Content.ReadAsStreamAsync(ct);
    using var reader = new StreamReader(stream);
    // Parse event:/data: lines and update UI from deserialized payload.
}
```

For .NET 10 SSE, use the `/net10` endpoint instead of `/net9`.

Minimal SSE endpoints in `Program.cs`:

```csharp
// net9-compatible SSE endpoint
app.MapGet("/api/uploads/progress/{uploadId}/net9", async (HttpContext context, string uploadId, CancellationToken ct) => {
    context.Response.Headers.ContentType = "text/event-stream";
    await context.Response.StartAsync(ct);
    // read events from channel and write:
    // event: progress
    // data: { ...json... }
});

#if NET10_0_OR_GREATER
// net10 SSE endpoint
app.MapGet("/api/uploads/progress/{uploadId}/net10", (string uploadId, CancellationToken ct)
    => TypedResults.ServerSentEvents(StreamProgressItems(uploadId, ct)));
#endif
```

## 3) SSR (non-interactive)

### Pure SSR (non-interactive)

Pure SSR is not interactive, so `NTInputFile` events (`OnUploadFile`, `OnProgressChanged`) do not run.
You can still use `NTInputFile` to retain NT styling and submit with a normal HTML form post.

```razor
@page "/upload-ssr"
@using NTComponents

<h3>SSR Upload</h3>

<form method="post" enctype="multipart/form-data" action="/api/uploads/process-ssr">
    <NTInputFile Label="Upload file"
                 ElementName="file"
                 MaximumFileCount="1"
                 MaximumFileSize="52428800"
                 ShowProgress="false" />
    <button type="submit">Upload</button>
</form>
```

```csharp
app.MapPost("/api/uploads/process-ssr", async (IFormFile file, CancellationToken ct) => {
    if (file.Length <= 0) {
        return Results.BadRequest("Empty file.");
    }

    var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "uploads");
    Directory.CreateDirectory(root);
    var path = Path.Combine(root, Path.GetFileName(file.FileName));

    await using var output = File.Create(path);
    await file.CopyToAsync(output, ct);
    return Results.Ok();
});
```
