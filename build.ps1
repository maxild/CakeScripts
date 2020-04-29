##########################################################################
# This is the Cake bootstrapper script for PowerShell.
# This file was downloaded from https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

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
.PARAMETER ShowVersion
Show version of Cake tool.
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
    [switch]$ShowVersion,
    [Alias("DryRun", "Noop")]
    [Parameter(Position = 0, Mandatory = $false, ValueFromRemainingArguments = $true)]
    [string[]]$ScriptArgs
)

$PSScriptRoot = split-path -parent $MyInvocation.MyCommand.Definition;

# Tree
$TOOLS_DIR = Join-Path $PSScriptRoot "tools"

# TODO: Maybe refactor into build.config
$CakeVersion = "0.37.0"
$GitVersionVersion = "5.0.1"

# Make sure tools folder exists
if ((Test-Path $PSScriptRoot) -and (-not (Test-Path $TOOLS_DIR))) {
    Write-Verbose -Message "Creating tools directory..."
    New-Item -Path $TOOLS_DIR -Type directory | out-null
}

###########################################################################
# INSTALL CAKE
###########################################################################

# Make sure Cake has been installed.
[string] $CakeExePath = ''
[string] $CakeInstalledVersion = Get-Command dotnet-cake -ErrorAction SilentlyContinue | ForEach-Object { &$_.Source --version }

if ($CakeInstalledVersion -eq $CakeVersion) {
    # Cake found locally
    $CakeExePath = (Get-Command dotnet-cake).Source
}
else {
    $CakePath = Join-Path $TOOLS_DIR '.store' | Join-Path -ChildPath 'cake.tool' | Join-Path -ChildPath $CakeVersion
    $CakePathExists = Test-Path -Path $CakePath -PathType Container

    $CakeExePath = (Get-ChildItem -Path $TOOLS_DIR -Filter "dotnet-cake*" -File | ForEach-Object FullName | Select-Object -First 1)
    $CakeExePathExists = (![string]::IsNullOrEmpty($CakeExePath)) -and (Test-Path $CakeExePath -PathType Leaf)

    if ((!$CakePathExists) -or (!$CakeExePathExists)) {

        if ($CakeExePathExists) {
            & dotnet tool uninstall --tool-path $TOOLS_DIR Cake.Tool
        }

        & dotnet tool install --tool-path $TOOLS_DIR --version $CakeVersion --configfile NuGet.public.config Cake.Tool
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }

        $CakeExePath = (Get-ChildItem -Path $TOOLS_DIR -Filter "dotnet-cake*" -File | ForEach-Object FullName | Select-Object -First 1)
    }
}

###########################################################################
# INSTALL GITVERSION
###########################################################################

$GitVersionToolPath = Join-Path $TOOLS_DIR '.store' | Join-Path -ChildPath 'gitversion.tool'

# ./.store/gitversion.tool/x.y.z that indicate that the gitversion tool have been installed (locally) into tools folder
if (![string]::IsNullOrEmpty($GitVersionVersion)) {
    $GitVersionPath = Join-Path $GitVersionToolPath $GitVersionVersion
    $GitVersionPathExists = Test-Path -Path $GitVersionPath -PathType Container
}
else {
    $GitVersionPathExists = $false
}

# ./dotnet-gitversion.exe that can be resolved as a tool by cake
$GitVersionExePath = (Get-ChildItem -Path $TOOLS_DIR -Filter "dotnet-gitversion*" -File | ForEach-Object FullName | Select-Object -First 1)
$GitVersionExePathExists = (![string]::IsNullOrEmpty($GitVersionExePath)) -and (Test-Path $GitVersionExePath -PathType Leaf)

if ((!$GitVersionPathExists) -or (!$GitVersionExePathExists)) {

    if ($GitVersionExePathExists) {
        & dotnet tool uninstall --tool-path $TOOLS_DIR GitVersion.Tool
    }

    if (![string]::IsNullOrEmpty($GitVersionVersion)) {
        & dotnet tool install --tool-path $TOOLS_DIR --version $GitVersionVersion --configfile NuGet.public.config GitVersion.Tool
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# When using modules we have to add this
& "$CakeExePath" ./build.cake --bootstrap

if ($ShowVersion.IsPresent) {
    & "$CakeExePath" --version
}
else {
    # Build the argument list.
    $Arguments = @{
        target        = $Target;
        configuration = $Configuration;
        verbosity     = $Verbosity;
    }.GetEnumerator() | ForEach-Object { "--{0}=`"{1}`"" -f $_.key, $_.value }

    Write-Host "Running build script..."
    & "$CakeExePath" ./build.cake $Arguments $ScriptArgs
}
exit $LASTEXITCODE
