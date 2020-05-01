#!/usr/bin/env pwsh

<#

.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
This Powershell script will ensure cake.tool and gitversion.tool are installed,
and execute your Cake build script with the parameters you provide.

.PARAMETER Target
The task/target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER ScriptArgs
Remaining arguments are added here.

.LINK
http://cakebuild.net

#>

[CmdletBinding()]
Param(
    [string]$Target = "Default",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity = "Verbose",
    [Parameter(Position = 0, Mandatory = $false, ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArgs
)

$PSScriptRoot = split-path -parent $MyInvocation.MyCommand.Definition;

###########################################################################
# LOAD versions from build.config
###########################################################################

[string] $DotNetSdkVersion = ''
[string] $CakeVersion = ''
[string] $GitVersionVersion = ''
foreach ($line in Get-Content (Join-Path $PSScriptRoot 'build.config')) {
    if ($line -like 'DOTNET_VERSION=*') {
        $DotNetSdkVersion = $line.SubString(15)
    }
    elseif ($line -like 'CAKE_VERSION=*') {
        $CakeVersion = $line.SubString(13)
    }
    elseif ($line -like 'GITVERSION_VERSION=*') {
        $GitVersionVersion = $line.SubString(19)
    }
}
if ([string]::IsNullOrEmpty($DotNetSdkVersion)) {
    'Failed to parse .NET Core SDK version'
    exit 1
}
if ([string]::IsNullOrEmpty($CakeVersion)) {
    'Failed to parse Cake version'
    exit 1
}
if ([string]::IsNullOrEmpty($GitVersionVersion)) {
    'Failed to parse GitVersion version'
    exit 1
}

###########################################################################
# Install .NET Core SDK
###########################################################################

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1 # Caching packages on a temporary build machine is a waste of time.
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1       # opt out of telemetry
$env:DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX = 2

$DotNetChannel = 'LTS'

Function Remove-PathVariable([string]$VariableToRemove) {
    $path = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($path -ne $null) {
        $newItems = $path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "User")
    }

    $path = [Environment]::GetEnvironmentVariable("PATH", "Process")
    if ($path -ne $null) {
        $newItems = $path.Split(';', [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { "$($_)" -inotlike $VariableToRemove }
        [Environment]::SetEnvironmentVariable("PATH", [System.String]::Join(';', $newItems), "Process")
    }
}

$FoundDotNetSdkVersion = $null
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    # dotnet --version will use version found in global.json, but the SDK will error if the
    # global.json version is not found on the machine.
    $FoundDotNetSdkVersion = & dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        # Extract the first line of the message without making powershell write any error messages
        Write-Host ($FoundDotNetSdkVersion | ForEach-Object { "$_" } | select-object -first 1)
        Write-Host "That is not problem, we will install the SDK version below."
        $FoundDotNetSdkVersion = "" # Force installation of .NET Core SDK via dotnet-install script
    }
    else {
        Write-Host ".NET Core SDK version $FoundDotNetSdkVersion found."
    }
}

if ($FoundDotNetSdkVersion -ne $DotNetSdkVersion) {
    Write-Verbose -Message "Installing .NET Core SDK version $DotNetSdkVersion ..."

    $InstallPath = Join-Path $PSScriptRoot ".dotnet"
    if (-not (Test-Path $InstallPath)) {
        mkdir -Force $InstallPath | Out-Null
    }

    (New-Object System.Net.WebClient).DownloadFile("https://dot.net/v1/dotnet-install.ps1", "$InstallPath\dotnet-install.ps1")

    & $InstallPath\dotnet-install.ps1 -Channel $DotNetChannel -Version $DotNetSdkVersion -InstallDir $InstallPath -NoPath

    Remove-PathVariable "$InstallPath"
    $env:PATH = "$InstallPath;$env:PATH"
    $env:DOTNET_ROOT = $InstallPath
}

###########################################################################
# INSTALL .NET Core 3.x tools
###########################################################################

$TOOLS_DIR = Join-Path $PSScriptRoot "tools"
if ((Test-Path $PSScriptRoot) -and (-not (Test-Path $TOOLS_DIR))) {
    Write-Verbose -Message "Creating tools directory..."
    New-Item -Path $TOOLS_DIR -Type directory | out-null
}

# To see list of packageid, version and commands
#      dotnet tool list --tool-path ./tools
Function Install-NetCoreTool {
    param
    (
        [string]$PackageId,
        [string]$ToolCommandName,
        [string]$Version
    )

    $ToolPath = Join-Path $TOOLS_DIR '.store' | Join-Path -ChildPath $PackageId.ToLower() | Join-Path -ChildPath $Version
    $ToolPathExists = Test-Path -Path $ToolPath -PathType Container

    $ExePath = (Get-ChildItem -Path $TOOLS_DIR -Filter "${ToolCommandName}*" -File | ForEach-Object FullName | Select-Object -First 1)
    $ExePathExists = (![string]::IsNullOrEmpty($ExePath)) -and (Test-Path $ExePath -PathType Leaf)

    if ((!$ToolPathExists) -or (!$ExePathExists)) {

        if ($ExePathExists) {
            & dotnet tool uninstall --tool-path $TOOLS_DIR GitReleaseManager.Tool | Out-Null
        }

        & dotnet tool install --tool-path $TOOLS_DIR --version $Version --configfile NuGet.public.config $PackageId | Out-Null
        if ($LASTEXITCODE -ne 0) {
            "Failed to install $PackageId"
            exit $LASTEXITCODE
        }

        $ExePath = (Get-ChildItem -Path $TOOLS_DIR -Filter "${ToolCommandName}*" -File | ForEach-Object FullName | Select-Object -First 1)
    }

    return $ExePath
}

[string] $CakeExePath = Install-NetCoreTool -PackageId 'Cake.Tool' -ToolCommandName 'dotnet-cake' -Version $CakeVersion
Install-NetCoreTool -PackageId 'GitVersion.Tool' -ToolCommandName 'dotnet-gitversion' -Version $GitVersionVersion | Out-Null

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# When using modules we have to add this
& "$CakeExePath" ./build.cake --bootstrap

# Build the argument list.
$Arguments = @{
    target        = $Target;
    configuration = $Configuration;
    verbosity     = $Verbosity;
}.GetEnumerator() | ForEach-Object { "--{0}=`"{1}`"" -f $_.key, $_.value }

Write-Host "Running build script..."
& "$CakeExePath" ./build.cake $Arguments $ScriptArgs
exit $LASTEXITCODE
