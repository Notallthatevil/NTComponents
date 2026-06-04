# NTComponents
[![Deploy](https://github.com/Notallthatevil/NTComponents/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/Notallthatevil/NTComponents/actions/workflows/ci-cd.yml)
[![Publish AOTCompatibility](https://github.com/Notallthatevil/NTComponents/actions/workflows/ensure-aot-build.yml/badge.svg)](https://github.com/Notallthatevil/NTComponents/actions/workflows/ensure-aot-build.yml)

NTComponents is a Blazor WebAssembly project that provides a set of reusable UI components for building modern web applications based on Google's Material 3 spec. The components are designed to be highly customizable and easy to use.

## Features

- **Form Components**: Includes various form components like `TnTInputFile` with advanced features.
- **Toast Notifications**: Provides a service for displaying toast notifications with different styles and messages.
- **Theming**: Supports theming with customizable color schemes and styles.
- **Grid**: A data grid component modified from FluentDataGrid.
- **Scheduler**: A scheduler component with week view and event management.

## Toast Notifications

Place one `NTToast` near the route or layout level. The component is static-rendering friendly and loads the JavaScript bridge used by both browser JavaScript and `INTToastService`.

```razor
<NTToast />
```

Use `INTToastService` from interactive Blazor code:

```razor
@inject INTToastService ToastService

<button @onclick="SaveAsync">Save</button>

@code {
    private async Task SaveAsync() {
        await ToastService.ShowSuccessAsync("Saved", "Your changes were saved.");
    }
}
```

For static SSR markup or native HTML handlers, call the JavaScript bridge directly and guard the call in case the module has not loaded yet:

```html
<button onclick="window.NTToast?.queueToast({ title: 'Saved', message: 'Your changes were saved.', variant: 'success' })">
    Save
</button>
```

JavaScript options are `title`, `message`, `variant`, `timeout`, `showClose`, `icon`, `backgroundColor`, `textColor`, and `iconColor`. Variants are `default`, `success`, `info`, `warning`, `error`, and `assert`.

Best practices:
- Use `NTToast` with `INTToastService`; legacy `TnTToast` uses `ITnTToastService` and is a separate host/service pair.
- Prefer semantic helpers such as `ShowSuccessAsync`, `ShowInfoAsync`, `ShowWarningAsync`, `ShowErrorAsync`, and `ShowAssertAsync`.
- Keep toast copy short and status-focused.
- Let the default four-second timeout handle normal messages. Use `timeout: 0` only when explicit dismissal is required.
- Use `error` or `assert` only for high-priority messages because they use assertive accessibility announcements.

## Getting Started

### Prerequisites

- .NET 9 or .NET 10 SDK

### Install

Install from NuGet (package id: `NTComponents`):

```
dotnet add package NTComponents
```

Or add the package reference in your project file.

### Building the Project

1. Restore the NuGet packages:
```
dotnet restore
```
2. Build the solution:
```
dotnet build
```

### Usage
In your `Program.cs` file add the following to register any library services (see `LiveTest` for examples):

```csharp
// builder is the WebAssemblyHostBuilder or WebApplicationBuilder
builder.Services.AddNTComponents();
```

Then use components in your pages (see `LiveTest` samples for exact component names and parameters):

```razor
@page "/"
<h3>Example</h3>
<TnTButton OnClick="() => Console.WriteLine("Clicked")">Click me</TnTButton>
```

### Theming
Themes can be generated using Google's Material 3 designer. Export your theme as a json file and drop it in the `wwwroot` folder. Inside your `App.razor` file, add the following code:

```razor
<NTComponents.TnTThemeDesign ThemeFile="your-theme.json" />
```

Dark, light, and system themes can be applied by setting the `Theme` property of the `TnTThemeDesign` component.

## Contributing

Contributions are welcome! 

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

