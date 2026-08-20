[CmdletBinding()]
param(
    [int]$Port = 5090,
    [switch]$SkipBuild,
    [switch]$Open
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$buildOutput = Join-Path $repoRoot "artifacts\cypress-build"
$baseUrl = "http://127.0.0.1:$Port"
$server = $null

Push-Location $repoRoot
try {
    if (-not (Test-Path "node_modules\cypress")) {
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install failed." }
    }

    if (-not $SkipBuild) {
        dotnet build "DailyVitals.Web\DailyVitals.Web.csproj" -o $buildOutput
        if ($LASTEXITCODE -ne 0) { throw "DailyVitals web build failed." }
    }

    $serverEnvironment = @{
        ASPNETCORE_ENVIRONMENT = "Development"
        DemoMode__Enabled = "true"
        DemoMode__UserName = "demo@activevitals.app"
        DemoMode__Password = "Demo123!"
    }

    $server = Start-Process dotnet `
        -ArgumentList "DailyVitals.Web.dll", "--urls", $baseUrl `
        -WorkingDirectory $buildOutput `
        -Environment $serverEnvironment `
        -WindowStyle Hidden `
        -PassThru

    $deadline = (Get-Date).AddSeconds(30)
    do {
        try {
            $response = Invoke-WebRequest -UseBasicParsing "$baseUrl/signin" -TimeoutSec 2
            if ($response.StatusCode -eq 200) { break }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    if (-not $response -or $response.StatusCode -ne 200) {
        throw "DailyVitals did not become ready at $baseUrl."
    }

    $env:ELECTRON_RUN_AS_NODE = $null
    $env:CYPRESS_BASE_URL = $baseUrl
    if ($Open) {
        npm run cy:open
    }
    else {
        npm run test:e2e
    }
    if ($LASTEXITCODE -ne 0) { throw "Cypress reported one or more failures." }
}
finally {
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id
    }
    Pop-Location
}
