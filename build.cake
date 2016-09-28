///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////
#tool "nuget:?package=gitreleasemanager&version=0.6.0"

///////////////////////////////////////////////////////////////////////////////
// SCRIPTS
///////////////////////////////////////////////////////////////////////////////
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

    // TODO: Addin the release notes in the nuspec
    // ReleaseNotes = parameters.ReleaseNotes.Notes.ToArray(),

    NuGetPack(nuspecFile, new NuGetPackSettings {
        Version = parameters.VersionInfo.NuGetVersion,
        OutputDirectory = parameters.Paths.Directories.Artifacts
    });
});

// appveyor PushArtifact <path> [options] (See https://www.appveyor.com/docs/build-worker-api/#push-artifact)
Task("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Package")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor)
    .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.Artifacts))
    .Does(() =>
{
    var nupkgFile = GetFiles(parameters.Paths.Directories.Artifacts + "/*.nupkg").Single();
    AppVeyor.UploadArtifact(nupkgFile);

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

    var nupkgFile = GetFiles(parameters.Paths.Directories.Artifacts + "/*.nupkg").Single();

    NuGetPush(nupkgFile, new NuGetPushSettings {
        Source = parameters.ProdFeed.SourceUrl,
        ApiKey = parameters.ProdFeed.ApiKey
    });
})
.OnError(exception =>
{
    Information("Publish-Packages Task failed, but continuing with next Task...");
    publishingError = true;
});

//
// Release Management on GitHub: issues, tags and milestones.
//
// Create a draft set of release notes based on a milestone, which has been set up in GitHub.
// Using the generated milestone (=version), create a draft release on GitHub
// This set of release notes is created in draft format, ready for review, in the GitHub UI.
Task("CreateGitHubReleaseNotes")
    .Does(() =>
{
    Information("Creating GitHub Release Notes...");
    // GitReleaseManager.exe create
    //    -milestone $script:version -targetDirectory $rootDirectory -targetcommitish master
    //    -u GitHubUserName -p GitHubPassword
    //    -o repoOwner -r repoName
    GitReleaseManagerCreate(parameters.GitHub.UserName, parameters.GitHub.Password,
                            parameters.Git.RepositoryOwner, parameters.Git.RepositoryName,
        new GitReleaseManagerCreateSettings {
            Milestone         = parameters.VersionInfo.Milestone,
            Name              = parameters.VersionInfo.SemVer, // -name
            Prerelease        = true,    // create the release as a prerelease
            TargetCommitish   = "master" // The commit to tag
        });
});

// Manual workflow:
// 1) The build artifacts which have been deployed to RCFeed are tested.
// 2) The release notes are reviewed, and ensured to be correct.
// 3) Assuming that everything is verified to be correct, the draft release is then published
//    through the GitHub UI, which creates a tag in the repository, triggering another AppVeyor build,
//    this time with deployment to ProdFeed.

// During this build, GitReleaseManager is executed using the export command, so that all release notes can be bundled into the application
Task("ExportGitHubReleaseNotes")
    .Does(() =>
{
    // Export all the release notes for a given repository on GitHub. The generated file will be in Markdown format, and the contents of the
    // file is configurable using the GitReleaseManager.yaml file, per repository.

    // GitReleaseManager.exe export
    //    -fileOutputPath ./CHANGELOG.md -targetDirectory $rootDirectory
    //    -u $env:GitHubUserName -p $env:GitHubPassword
    //    -o chocolatey -r chocolateygui

    // Note .CHANGELOG.md is just a temporary file used on appveyor, and it contains the following text on GitHub:
    //
    // This file will be updated as part of the ChocolateyGUI build process.
    //
    // If you want to see the current release notes, please check [here](https://github.com/chocolatey/ChocolateyGUI/releases)
});

// In addition, GitReleaseManager is executed using the addasset command to add the build artifacts to the GitHub release - source
Task("AddAssetsToGitHubRelease")
    .Does(() =>
{
    // Once a draft set of release notes has been created, it is possible to add additional assets to the release using the addasset command.

    // GitReleaseManager.exe addasset
    //     -assets $convertedPath -tagName $script:version -targetDirectory $rootDirectory
    //     -u $env:GitHubUserName -p $env:GitHubPassword
    //     -o chocolatey -r chocolateygui
});

// And finally, GitReleaseManager is executed using the close command to close the milestone associated with the release that has just been published
Task("CloseMilestone")
    .Does(() =>
{
    // GitReleaseManager.exe close
    //     -milestone $script:version -targetDirectory $rootDirectory
    //     -u $env:GitHubUserName -p $env:GitHubPassword
    //     -o chocolatey -r chocolateygui
});

/////

Task("Publish-GitHub-Release")
    .IsDependentOn("Package")
    .WithCriteria(() => parameters.DeployToProdFeed) // Tag push of commit on master branch on AppVeyor
    .Does(() =>
{
    if (DirectoryExists(parameters.Paths.Directories.Artifacts))
    {
        foreach (var nupkgFile in GetFiles(parameters.Paths.Directories.Artifacts + "/*.nupkg"))
        {
            GitReleaseManagerAddAssets(parameters.GitHub.UserName, parameters.GitHub.Password,
                                       parameters.Git.RepositoryOwner, parameters.Git.RepositoryName,
                                       parameters.VersionInfo.Milestone, nupkgFile.ToString());
        }
    }

    // Close milestone
    GitReleaseManagerClose(parameters.GitHub.UserName, parameters.GitHub.Password,
                           parameters.Git.RepositoryOwner, parameters.Git.RepositoryName,
                           parameters.VersionInfo.Milestone);
})
.OnError(exception =>
{
    Information("Publish-GitHub-Release Task failed, but continuing with next Task...");
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
    .IsDependentOn("Publish-GitHub-Release")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of " + parameters.Project.Name + ".  All publishing tasks have been attempted.");
    }
});

// Task("ClearCache")
//   .IsDependentOn("Clear-AppVeyor-Cache");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(parameters.Target);
