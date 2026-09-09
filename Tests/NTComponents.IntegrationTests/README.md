# Browser regression tests

The Playwright fixture defaults to Chromium. Set `NTCOMPONENTS_TEST_BROWSER` to
`chromium`, `firefox`, or `webkit` to select another engine for a test process.
Each test receives a fresh browser context; unsupported names fail the run.

From the repository root, build the integration project and install the matching
Playwright browser versions:

```powershell
dotnet build Tests/NTComponents.IntegrationTests/NTComponents.IntegrationTests.csproj --framework net11.0
pwsh Tests/NTComponents.IntegrationTests/bin/Debug/net11.0/playwright.ps1 install chromium firefox webkit
```

Run the grid regressions in each engine:

```powershell
try {
    foreach ($browserName in @('chromium', 'firefox', 'webkit')) {
        $env:NTCOMPONENTS_TEST_BROWSER = $browserName
        dotnet test --project Tests/NTComponents.IntegrationTests/NTComponents.IntegrationTests.csproj --framework net11.0 --no-build --filter-class 'NTComponents.IntegrationTests.Grid.*'
        if ($LASTEXITCODE -ne 0) { throw "Grid regressions failed in $browserName" }
    }
}
finally {
    Remove-Item Env:NTCOMPONENTS_TEST_BROWSER -ErrorAction SilentlyContinue
}
```

The reusable build workflow runs the normal integration suite in Chromium.
Firefox/WebKit runs are available locally using the commands above. Linux
installations also need Playwright's `install --with-deps` option.

The JavaScript suite (`npm test`) includes a deterministic 100,000-item stress
case covering forward/reverse windows, changing detail heights, measurement
resets, viewport coverage, request limits, and observer/timer cleanup. It uses
an independent geometry oracle; it does not rely on a wall-clock timing limit.
