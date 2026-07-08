[CmdletBinding()]
param(
    [string]$Runtime = "win-x64",

    [string]$Browsers = "chromium",

    [string]$VersionSuffix = $env:PACKAGE_VERSION
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Project = "PlaywrightRunner"
$ProjectPath = "src\${Project}\${Project}.csproj"
$FlowFile = "saucedemo.yaml"
$Configuration = "Release"
$Framework = "net10.0"
$BrowsersNormalized = $Browsers.ToLowerInvariant()

$OutputRoot = "artifacts"
$PublishRoot = Join-Path $OutputRoot "publish"
$ZipDir = Join-Path $OutputRoot "zips"

switch ($BrowsersNormalized) {
    { $_ -in @("chromium", "chrome") } {
        $PlaywrightBrowsers = @("chromium")
        $BrowserZipSuffix = "chrome"
        break
    }
    "firefox" {
        $PlaywrightBrowsers = @("firefox")
        $BrowserZipSuffix = "firefox"
        break
    }
    "webkit" {
        $PlaywrightBrowsers = @("webkit")
        $BrowserZipSuffix = "webkit"
        break
    }
    "all" {
        $PlaywrightBrowsers = @("chromium", "firefox", "webkit")
        $BrowserZipSuffix = "all"
        break
    }
    default {
        Write-Error "Unsupported browser bundle: $Browsers"
        Write-Error "Use one of: chromium, chrome, firefox, webkit, all"
        exit 2
    }
}

$PublishDir = Join-Path $PublishRoot "$Runtime-$BrowserZipSuffix"

$ZipBaseName = "$Project-$Runtime-$BrowserZipSuffix"
if (-not [string]::IsNullOrWhiteSpace($VersionSuffix)) {
    $ZipBaseName = "$ZipBaseName-$VersionSuffix"
}

$ZipFile = Join-Path $ZipDir "$ZipBaseName.zip"

Write-Host "Runtime: $Runtime"
Write-Host "Browsers: $Browsers"

Write-Host "Cleaning publish output..."
if (-not (Test-Path $ProjectPath)) {
    Write-Error "Project file not found: $ProjectPath"
    exit 2
}

# Important: do NOT remove $OutputRoot here.
# $ZipDir lives under $OutputRoot, so deleting $OutputRoot on every run
# deletes ZIPs created by previous matrix iterations.
if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $ZipDir -Force | Out-Null

Write-Host "Restoring..."
dotnet restore $ProjectPath

Write-Host "Building..."
dotnet build $ProjectPath `
    -c $Configuration `
    --no-restore

Write-Host "Testing..."
dotnet test $ProjectPath `
    -c $Configuration `
    --no-build

Write-Host "Publishing..."
dotnet publish $ProjectPath `
    -c $Configuration `
    -f $Framework `
    -r $Runtime `
    --self-contained true `
    -o $PublishDir `
    /p:PublishSingleFile=false `
    /p:PublishTrimmed=false

Write-Host "Installing bundled Playwright browsers: $($PlaywrightBrowsers -join ' ')"
Push-Location $PublishDir
try {
    if (-not (Test-Path ".\playwright.ps1")) {
        Write-Error "Playwright install script not found in publish output: $PublishDir\playwright.ps1"
        exit 2
    }

    $PreviousPlaywrightBrowsersPath = $env:PLAYWRIGHT_BROWSERS_PATH
    $env:PLAYWRIGHT_BROWSERS_PATH = Join-Path (Get-Location) "ms-playwright"

    & ".\playwright.ps1" install @PlaywrightBrowsers
}
finally {
    if ($null -eq $PreviousPlaywrightBrowsersPath) {
        Remove-Item Env:\PLAYWRIGHT_BROWSERS_PATH -ErrorAction SilentlyContinue
    }
    else {
        $env:PLAYWRIGHT_BROWSERS_PATH = $PreviousPlaywrightBrowsersPath
    }

    Pop-Location
}

if (Test-Path $FlowFile) {
    Write-Host "Copying sample flow..."
    Copy-Item $FlowFile $PublishDir -Force
}

Write-Host "Zipping..."
if (Test-Path $ZipFile) {
    Remove-Item $ZipFile -Force
}

Compress-Archive `
    -Path (Join-Path $PublishDir "*") `
    -DestinationPath $ZipFile `
    -Force

Write-Host "Done:"
Write-Host $ZipFile
