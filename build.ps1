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

[string] $CakeVersion = ''
[string] $GitVersionVersion = ''
foreach ($line in Get-Content (Join-Path $PSScriptRoot 'build.config')) {
    if ($line -like 'CAKE_VERSION=*') {
        $CakeVersion = $line.SubString(13)
    }
    elseif ($line -like 'GITVERSION_VERSION=*') {
        $GitVersionVersion = $line.SubString(19)
    }
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
