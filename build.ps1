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
# INSTALL .NET Core 3.x tools
###########################################################################

# To see list of packageid, version and commands
#      dotnet tool list --tool-path ./tools
Function Install-NetCoreTool {
    param
    (
        [string]$PackageId,
        [string]$ToolCommandName,
        [string]$Version
    )

    if (![string]::IsNullOrEmpty($Version)) {
        $ToolPath = Join-Path $TOOLS_DIR '.store' | Join-Path -ChildPath $PackageId.ToLower() | Join-Path -ChildPath $Version
        $ToolPathExists = Test-Path -Path $ToolPath -PathType Container
    }
    else {
        $ToolPathExists = $false
    }

    $ExePath = (Get-ChildItem -Path $TOOLS_DIR -Filter "${ToolCommandName}*" -File | ForEach-Object FullName | Select-Object -First 1)
    $ExePathExists = (![string]::IsNullOrEmpty($ExePath)) -and (Test-Path $ExePath -PathType Leaf)

    if ((!$ToolPathExists) -or (!$ExePathExists)) {

        if ($ExePathExists) {
            & dotnet tool uninstall --tool-path $TOOLS_DIR GitReleaseManager.Tool
        }

        if (![string]::IsNullOrEmpty($Version)) {
            & dotnet tool install --tool-path $TOOLS_DIR --version $Version --configfile NuGet.public.config $PackageId
            if ($LASTEXITCODE -ne 0) {
                exit $LASTEXITCODE
            }
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
