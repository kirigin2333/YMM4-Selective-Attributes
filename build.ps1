param(
    [Parameter(Mandatory = $true)]
    [string]$YMM4DirPath
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$installDir = (Resolve-Path $YMM4DirPath).Path
if (-not $installDir.EndsWith("\")) {
    $installDir += "\"
}

$requiredFiles = @(
    "YukkuriMovieMaker.exe",
    "YukkuriMovieMaker.dll",
    "YukkuriMovieMaker.Plugin.dll",
    "YukkuriMovieMaker.Controls.dll",
    "NAudio.dll",
    "NAudio.Core.dll",
    "NAudio.WinMM.dll",
    "SharpGen.Runtime.dll",
    "SharpGen.Runtime.COM.dll",
    "Vortice.Direct2D1.dll",
    "Vortice.DirectX.dll",
    "Vortice.Mathematics.dll",
    "Newtonsoft.Json.dll"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $installDir $file
    if (-not (Test-Path $path)) {
        throw "Required YMM4 file not found: $path"
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw "The .NET 10 SDK is required. Install the .NET 10 SDK, then run this script again. Visual Studio is not required."
}

$sdkList = & dotnet --list-sdks
if (-not ($sdkList | Where-Object { $_ -match '^10\.' })) {
    throw "The .NET 10 SDK was not found. Install the .NET 10 SDK, then run this script again."
}

$escapedPath = [System.Security.SecurityElement]::Escape($installDir)
$props = @"
<Project>
  <PropertyGroup>
    <YMM4DirPath>$escapedPath</YMM4DirPath>
  </PropertyGroup>
</Project>
"@

Set-Content -Path (Join-Path $repoRoot "Directory.Build.props") -Value $props -Encoding UTF8

Push-Location $repoRoot
try {
    & dotnet build ".\src\YMM4SelectiveAttributes\YMM4SelectiveAttributes.csproj" -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
    Write-Host ""
    Write-Host "Build complete. Check YMM4 user\plugin\YMM4SelectiveAttributes\ for the plugin DLL."
}
finally {
    Pop-Location
}
