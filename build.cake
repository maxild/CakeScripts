// Load all scripts to check compilation
#load "content/appveyor.cake"
#load "content/credentials.cake"
#load "content/gitreleasemanager.cake"
#load "content/gitversion.cake"
#load "content/parameters.cake"
#load "content/paths.cake"

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;

///////////////////////////////////////////////////////////////
// Parameters (target, configuration etc.)
BuildParameters parameters = BuildParameters.GetParameters(Context);

///////////////////////////////////////////////////////////////
// Versioning
BuildVersion versionInfo = BuildVersion.Calculate(Context, parameters);

///////////////////////////////////////////////////////////////
// Configuration (Note: branch of dotnet cli is '1.0.0-preview2')
var settings = new BuildSettings {
    ArtifactsFolder = "artifacts",
    SrcFolder = "src",
    TestFolder = "test",
    BuildToolsFolder = ".tools",
    BuildScriptsFolder = "build",
    UseSystemDotNetPath = false,
    DotNetCliFolder = ".dotnet",
    DotNetCliInstallScriptUrl = "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0-preview2/scripts/obtain",
    DotNetCliChannel = "preview",
    DotNetCliVersion = "1.0.0-preview2-003121"
};
var paths = BuildPaths.GetPaths(Context, settings);

// Tools (like aliases)
// TODO: Use Cake Tools framework (ToolsLocator etc..)
string dotnet = settings.UseSystemDotNetPath
            ? "dotnet"
            : System.IO.Path.Combine(paths.DotNetCli, "dotnet");
string nuget = System.IO.Path.Combine(paths.BuildTools, "nuget");

///////////////////////////////////////////////////////////////
// Tasks

Task("Default")
    .Does(() =>
{
    Information("Build succeeded!");
});

/// <summary>
///  Install the .NET Core SDK Binaries (preview2 bits).
/// </summary>
Task("InstallDotNet")
    .Does(() =>
{
    Information("Installing .NET Core SDK Binaries...");

    var ext = IsRunningOnWindows() ? "ps1" : "sh";
    var installScript = string.Format("dotnet-install.{0}", ext);
    var installScriptDownloadUrl = string.Format("{0}/{1}", settings.DotNetCliInstallScriptUrl, installScript);
    var dotnetInstallScript = System.IO.Path.Combine(paths.DotNetCli, installScript);

    CreateDirectory(paths.DotNetCli);

    // TODO: wget(installScriptDownloadUrl, dotnetInstallScript)
    using (WebClient client = new WebClient())
    {
        client.DownloadFile(installScriptDownloadUrl, dotnetInstallScript);
    }

    if (IsRunningOnUnix())
    {
        Shell(string.Format("chmod +x {0}", dotnetInstallScript));
    }

    // Run the dotnet-install.{ps1|sh} script.
    // Note: The script will bypass if the version of the SDK has already been downloaded
    Shell(string.Format("{0} -Channel {1} -Version {2} -InstallDir {3} -NoPath", dotnetInstallScript, settings.DotNetCliChannel, settings.DotNetCliVersion, paths.DotNetCli));

    var dotNetExe = IsRunningOnWindows() ? "dotnet.exe" : "dotnet";
    if (!FileExists(System.IO.Path.Combine(paths.DotNetCli, dotNetExe)))
    {
        throw new Exception(string.Format("Unable to find {0}. The dotnet CLI install may have failed.", dotNetExe));
    }

    try
    {
        Run(dotnet, "--info");
    }
    catch
    {
        throw new Exception("dotnet --info have failed to execute. The dotnet CLI install may have failed.");
    }

    Information(".NET Core SDK install was succesful!");
});

RunTarget(parameters.Target);
