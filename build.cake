// Load all scripts to check compilation
//#load "src/gitreleasemanager.cake"
#load "src/utils.cake"
#load "src/gitversioninfo.cake"
#load "src/gitrepoinfo.cake"
#load "src/parameters.cake"
#load "src/settings.cake"
#load "src/paths.cake"

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var parameters = BuildParameters.GetParameters(
    Context,            // ICakeContext
    BuildSystem,        // BuildSystem alias
    new BuildSettings   // My personal overrides
    {
        DeployToProdFeed = _ => true,
        DeployToProdSourceUrl = @"https://www.myget.org/F/maxfire/api/v2/package"
    },
    new BuildPathSettings
    {
        NuspecDir = "."     // override default './nuspec' value
    });
var publishingError = false;

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

Task("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Package")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor)
    .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.Artifacts))
    .Does(() =>
{
    foreach (var nupkgFile in GetFiles(parameters.Paths.Directories.Artifacts + "/*.nupkg"))
    {
        // appveyor PushArtifact <path> [options] (See https://www.appveyor.com/docs/build-worker-api/#push-artifact)
        AppVeyor.UploadArtifact(nupkgFile);
    }
});

Task("Publish-Packages")
    .IsDependentOn("Package")
    .WithCriteria(() => parameters.DeployToProdFeed)
    .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.Artifacts))
    .Does(() =>
{
    if (string.IsNullOrEmpty(parameters.ProdFeed.ApiKey))
    {
        throw new InvalidOperationException("Could not resolve NuGet push API key.");
    }

    if (string.IsNullOrEmpty(parameters.ProdFeed.SourceUrl))
    {
        throw new InvalidOperationException("Could not resolve NuGet push URL.");
    }

    var nupkgFiles = GetFiles(parameters.Paths.Directories.Artifacts + "*.nupkg");

    NuGetPush(nupkgFiles, new NuGetPushSettings {
        Source = parameters.ProdFeed.SourceUrl,
        ApiKey = parameters.ProdFeed.ApiKey
    });
})
.OnError(exception =>
{
    Information("Publish-Packages Task failed, but continuing with next Task...");
    publishingError = true;
});

Task("Default")
    .IsDependentOn("Show-Info")
    .IsDependentOn("Print-AppVeyor-Environment-Variables")
    .IsDependentOn("Package");

Task("AppVeyor")
    .IsDependentOn("Show-Info")
    .IsDependentOn("Print-AppVeyor-Environment-Variables")
    .IsDependentOn("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Publish-Packages")
    //.IsDependentOn("Publish-GitHub-Release")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of " + parameters.Project.Name + ".  All publishing tasks have been attempted.");
    }
});

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(parameters.Target);
