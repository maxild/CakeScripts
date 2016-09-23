// Load all scripts to check compilation
//#load "content/gitreleasemanager.cake"
#load "content/utils.cake"
#load "content/gitversioninfo.cake"
#load "content/gitrepoinfo.cake"
#load "content/parameters.cake"
#load "content/settings.cake"
#load "content/environment.cake"
#load "content/paths.cake"

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var parameters = BuildParameters.GetParameters(
    Context,            // ICakeContext
    BuildSystem,        // BuildSystem alias
    new BuildSettings   // My personal overrides
    {
        EnvironmentVariableNames = new EnvironmentVariableNames
        {
            GitHubPasswordVariable = "github_password"
        }
    },
    new BuildPathSettings
    {
        NuspecDir = "."     // override default './nuspec' value
    });

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    if (parameters.Git.IsMasterBranch && context.Log.Verbosity != Verbosity.Diagnostic) {
        Information("Increasing verbosity to diagnostic.");
        context.Log.Verbosity = Verbosity.Diagnostic;
    }

    Information("Building version {0} of {1} ({2}, {3}) using version {4} of Cake. (IsTagPush: {5})",
        parameters.VersionInfo.SemVer,
        parameters.Project.Name,
        parameters.Configuration,
        parameters.Target,
        parameters.VersionInfo.CakeVersion,
        parameters.IsTagPush);
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clear-Artifacts")
    .Does(() =>
{
    if (DirectoryExists(parameters.Paths.Directories.Artifacts))
    {
        DeleteDirectory(parameters.Paths.Directories.Artifacts, true);
    }
});

Task("Show-Info")
    .Does(() =>
{
    parameters.PrintToLog();
});

Task("Print-AppVeyor-Environment-Variables")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    parameters.PrintAppVeyorEnvironmentVariables();
});

Task("Package")
    .IsDependentOn("Clear-Artifacts")
    .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.Nuspec))
    .Does(() =>
{
    EnsureDirectoryExists(parameters.Paths.Directories.Artifacts);

    var nuspecFile = GetFiles(parameters.Paths.Directories.Nuspec + "/*.nuspec").Single();

    NuGetPack(nuspecFile, new NuGetPackSettings {
        Version = parameters.VersionInfo.NuGetVersion,
        OutputDirectory = parameters.Paths.Directories.Artifacts
    });
});

// Task("Publish-MyGet-Packages")
//     .IsDependentOn("Package")
//     .WithCriteria(() => !parameters.IsLocalBuild)
//     .WithCriteria(() => !parameters.IsPullRequest)
//     .WithCriteria(() => parameters.IsMainRepository)
//     .WithCriteria(() => parameters.IsTagPush || !parameters.IsMasterBranch)
//     .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.Artifacts))
//     .Does(() =>
// {
//     if (string.IsNullOrEmpty(parameters.MyGet.ApiKey))
//     {
//         throw new InvalidOperationException("Could not resolve MyGet API key.");
//     }

//     if (string.IsNullOrEmpty(parameters.MyGet.SourceUrl))
//     {
//         throw new InvalidOperationException("Could not resolve MyGet API url.");
//     }

//     var nupkgFile = GetFiles(parameters.Paths.Directories.Artifacts + "*.nupkg");

//     NuGetPush(nupkgFile, new NuGetPushSettings {
//         Source = parameters.MyGet.SourceUrl,
//         ApiKey = parameters.MyGet.ApiKey
//     });
// })
// .OnError(exception =>
// {
//     Information("Publish-MyGet-Packages Task failed, but continuing with next Task...");
//     publishingError = true;
// });

// Publish-GitHub-Release

Task("Default")
    .IsDependentOn("Show-Info")
    .IsDependentOn("Print-AppVeyor-Environment-Variables")
    .IsDependentOn("Package");

// Task("AppVeyor")
//     .IsDependentOn("Upload-AppVeyor-Artifacts")
//     .IsDependentOn("Publish-MyGet-Packages")
//     .IsDependentOn("Publish-Nuget-Packages")
//     .IsDependentOn("Publish-GitHub-Release")
//     .Finally(() =>
// {
//     if (publishingError)
//     {
//         throw new Exception("An error occurred during the publishing of " + parameters.Project.Name + ".  All publishing tasks have been attempted.");
//     }
// });

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(parameters.Target);
