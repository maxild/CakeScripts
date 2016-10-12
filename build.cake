///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////
#tool "nuget:?package=gitreleasemanager&version=0.6.0"

///////////////////////////////////////////////////////////////////////////////
// SCRIPTS (load all files to check compilation)
///////////////////////////////////////////////////////////////////////////////
#load "src/failurehelpers.cake"
#load "src/githubrepository.cake"
#load "src/gitrepoinfo.cake"
#load "src/gitversioninfo.cake"
#load "src/parameters.cake"
#load "src/paths.cake"
#load "src/projectjson.cake"
#load "src/settings.cake"
#load "src/toolrunner.cake"
#load "src/utils.cake"

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

// Here we call the (pseudo) private __GetParametersHelper__ method,
// because the way we generate the CakeScriptsVersion property in the
// 'Generate-CakeScripts-Version-Source-File' task.
var parameters = BuildParameters.__GetParametersHelper__(
    Context,            // ICakeContext
    BuildSystem,        // BuildSystem alias
    new BuildSettings   // My personal overrides
    {
        MainRepositoryOwner = "maxild",
        RepositoryName = "CakeScripts",
        DeployToProdFeed = _ => true,
        DeployToProdFeedUrl = @"https://www.myget.org/F/maxfire/api/v2/package"
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
        parameters.ProjectName,
        parameters.Configuration,
        parameters.Target,
        parameters.VersionInfo.CakeVersion,
        parameters.IsTagPush);
});

///////////////////////////////////////////////////////////////////////////////
// PRIMARY TASKS (direct targets)
///////////////////////////////////////////////////////////////////////////////

// Primary targets/tasks are short (= OneWord)
// Secondary tasks can have ny name (= Many-Words-Separated-By-Hyphens)

Task("Default")
    .IsDependentOn("Info")
    .IsDependentOn("Print-AppVeyor-Environment-Variables")
    .IsDependentOn("Package");

Task("AppVeyor")
    .IsDependentOn("Info")
    .IsDependentOn("Print-AppVeyor-Environment-Variables")
    .IsDependentOn("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Publish")
    //.IsDependentOn("Publish-GitHub-Release")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of " + parameters.ProjectName + ".  All publishing tasks have been attempted.");
    }
});

Task("ReleaseNotes")
  .IsDependentOn("Create-Release-Notes");

Task("Info")
    .Does(() =>
{
    parameters.PrintToLog();
});

Task("Clean")
    .Does(() =>
{
    parameters.ClearArtifacts();
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Generate-CakeScripts-Version-Source-File")
    .Does(() =>
{
    EnsureDirectoryExists(parameters.Paths.Directories.TempArtifacts);

    var allFile = parameters.Paths.Directories.TempArtifacts.CombineWithFilePath("all.cake");

    using (var allStream = System.IO.File.OpenWrite(allFile.FullPath))
    {
        foreach (var srcFile in GetFiles(parameters.Paths.Directories.Src + "/*.cake"))
        {
            var dstFile = parameters.Paths.Directories.TempArtifacts
                .CombineWithFilePath(srcFile.GetFilename());

            // copy file to ./artifacts/temp folder
            CopyFile(srcFile, dstFile);

            // write/concat file to ./artifacts/temp/all.cake
            using (var srcStream = System.IO.File.OpenRead(srcFile.FullPath))
            {
                srcStream.CopyTo(allStream);
            }
        }
    }
});

Task("Package")
    .IsDependentOn("Build")
    .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.Nuspec))
    .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.TempArtifacts))
    .Does(() =>
{
    var nuspecFile = GetFiles(parameters.Paths.Directories.Nuspec + "/*.nuspec").Single();

    // TODO: Addin the release notes in the nuspec
    // ReleaseNotes = parameters.ReleaseNotes.Notes.ToArray(),

    NuGetPack(nuspecFile, new NuGetPackSettings {
        Version = parameters.VersionInfo.NuGetVersion,
        OutputDirectory = parameters.Paths.Directories.Artifacts,
        BasePath = parameters.Paths.Directories.TempArtifacts
    });

    parameters.ClearTempArtifacts();
});

Task("Publish")
    .IsDependentOn("Package")
    .WithCriteria(() => parameters.ShouldDeployToProdFeed)
    .WithCriteria(() => DirectoryExists(parameters.Paths.Directories.Artifacts))
    .Does(() =>
{
    var nupkgFile = GetFiles(parameters.Paths.Directories.Artifacts + "/*.nupkg").Single();

    NuGetPush(nupkgFile, new NuGetPushSettings {
        Source = parameters.ProdFeed.GetRequiredSourceUrl(),
        ApiKey = parameters.ProdFeed.GetRequiredApiKey()
    });
})
.OnError(exception =>
{
    Information("Publish Task failed, but continuing with next Task...");
    publishingError = true;
});

//
// Release Management on GitHub: issues, tags and milestones.
//
// The standard workflow:
// For example, it is expected that the release notes will not be generated until you
// are literally at the point that the release is going to happen. i.e. you have closed
// all the issues, you have merged all the feature branches, etc. Then you run GitReleaseManager,
// make sure the notes are what you expect, and then you would publish the release,
// which could then potentially run your deployment as a result of finalising the release.

// Merge commit on master is created from release/hotfix branch. This is pushed to GitHub,
// and it triggers the 'Master' build at AppVeyor. I then check that this worked, and I am
// happy with the release and the notes. Then, through the GitHub Web UI, I publish the release,
// which adds the tag. This triggers the 'TagPush' build at AppVeyor. However, since this is a
// tagged build, it doesn't create a new release, it updates the existing one.
//
// When a GitHub release is first created, it is created as a draft release. As an admin on
// the repo, you can click "Edit" on the draft release, and there is a button at the bottom
// of the screen that opens up which is "Publish Release". That then adds the tag to the repo,
// and triggers the 'TagPush' build in AppVeyor.
//
// 2: So I guess your cake script does NOT invoke GitReleaseManagerCreate?
// 1: Not directly, no. That is something that I want to do only when I know I am ready. That
//    is the Create-Release-Notes task is invoked as a primary target locally on my machine, _after_
//    pushing the merge commit on master to GitHub. Otherwise, how would I know that I am ready
//    for a release to happen?
//    To be more precise, _after_ I have pushed master to GitHub, when I am ready to create a release,
//    I do '.\build.ps1 -target releasenotes' locally, which creates the (draft) release notes on
//    GitHub.

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
    // Note: Once you have an access token, you can enter it instead of your password
    //       when performing Git operations over HTTPS.
    // Note: Select the scopes you wish to grant to this access token (scope should be 'repo').
    // Note: Remember to keep your tokens secret; treat them just like passwords! They act on
    //       your behalf when interacting with the API. Don't hardcode them into your programs.
    //       Instead, opt to use them as environment variables.
    // Note: Scopes limit access for OAuth tokens. They do not grant any additional permission
    //       beyond that which the user already has.
    //       curl -H "Authorization: token OAUTH-TOKEN" https://api.github.com/users/maxild -I
    //       See also: https://developer.github.com/v3/oauth/#scopes
    GitReleaseManagerCreate(parameters.GitHub.UserName, parameters.GitHub.Password,
                            parameters.GitHub.RepositoryOwner, parameters.GitHub.RepositoryName,
        // Title
        // Description: is the markdown created by the tool, or imported from another tool.
        new GitReleaseManagerCreateSettings {
            Milestone         = parameters.VersionInfo.Milestone,
            Name              = parameters.VersionInfo.SemVer, // -name (Is this the release title)
            Prerelease        = true,    // create the release as a prerelease (can this be changed later?)
            TargetCommitish   = "master" // The commit/branch to tag (Choose an existing tag, or create a new tag on publish)
        });
    // Note: Users with push access to the repository can create/edit/delete a release.
    // Note: Information about published releases are available to everyone. Only users with
    //       push access will receive listings for draft releases.
    // Note: GitHub releases can be deleted later on..
});

// Manual workflow:
// 1) The build artifacts which have been deployed to RCFeed are tested.
// 2) The release notes are reviewed, and ensured to be correct.
// 3) Assuming that everything is verified to be correct, the draft release is then published
//    through the GitHub UI, which creates a tag in the repository, triggering another AppVeyor build,
//    this time with deployment to ProdFeed.

// About exporting release notes to nuspec (nuget package):
// 1: GRM has the concept of an export command, which will take the generated release notes from
// the GitHub release and place them into a markdown format, but that workflow requires the
// GitHub Release to be created in the first place. In this workflow, GitHub releases are the
// single source of truth.
// 2: But it would be neat to have it operate in an “upcoming milestone mode” for notes export,
// where it only exported what would end up in an upcoming release.
// 1: Yes, exporting the notes from a single milestone is something that I have thought about adding,
// rather than everything. I would be happy for you to create an issue to that effect.
// 2: I have never used milestones on GitHub before. Is that a prerequisite for GRM to operate (at all)?
// 1: Yes, milestones are a pre-requisite, unless you use something like GitReleaseNotes to
// generate the release notes, which would then be passed into GitReleaseManager as an input parameter.
// 1: There is a distinction between what I think you are calling a release (=deployment), and what
// GRM calls a release. In GRM a release is a created entry in GitHub, where the notes are created
// either from release notes passed in, or created from milestone issues in GitHub.
// 2: So it’s not possible to just have the notes be generated from the last tag, almost like how
// GitVersion operates?
// 1: Yes, you can, but that would be using the GitReleaseNotes tool, not GitReleaseManager. One
// creates the releasenotes (GitReleaseNotes) and another creates the release (GitReleaseManager)
// using the notes that are either passed in, or generated from milestone issues. again, two separate
// functions.

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

// Invoked locally as a primary target, when finalizing the merge commit on master (the release/master commit in GitFlow)
Task("Create-Release-Notes")
    .Does(() =>
{
    GitReleaseManagerCreate(parameters.GitHub.UserName, parameters.GitHub.Password,
                            parameters.GitHub.RepositoryOwner, parameters.GitHub.RepositoryName,
        new GitReleaseManagerCreateSettings
        {
            Milestone         = parameters.VersionInfo.Milestone,
            Name              = parameters.VersionInfo.Milestone,
            Prerelease        = true,
            TargetCommitish   = "master"
        });
});

// Invoked on AppVeyor, when tag is pushed ('Publish Release' on GitHub Web UI)
Task("Publish-GitHub-Release")
    .IsDependentOn("Package")
    .WithCriteria(() => parameters.ShouldDeployToProdFeed)
    .Does(() =>
{
    if (DirectoryExists(parameters.Paths.Directories.Artifacts))
    {
        foreach (var nupkgFile in GetFiles(parameters.Paths.Directories.Artifacts + "/*.nupkg"))
        {
            GitReleaseManagerAddAssets(parameters.GitHub.UserName, parameters.GitHub.Password,
                                       parameters.GitHub.RepositoryOwner, parameters.GitHub.RepositoryName,
                                       parameters.VersionInfo.Milestone, nupkgFile.ToString());
        }
    }

    // Close milestone
    GitReleaseManagerClose(parameters.GitHub.UserName, parameters.GitHub.Password,
                           parameters.GitHub.RepositoryOwner, parameters.GitHub.RepositoryName,
                           parameters.VersionInfo.Milestone);
})
.OnError(exception =>
{
    Information("Publish-GitHub-Release Task failed, but continuing with next Task...");
    publishingError = true;
});


// Task("ClearCache")
//   .IsDependentOn("Clear-AppVeyor-Cache");

///////////////////////////////////////////////////////////////////////////////
// SECONDARY TASKS (indirect targets)
///////////////////////////////////////////////////////////////////////////////

Task("Print-AppVeyor-Environment-Variables")
    .WithCriteria(AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    parameters.PrintAppVeyorEnvironmentVariables();
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

Task("Generate-CakeScripts-Version-Source-File")
    .Does(() =>
{
    // No heredocs in c#, so using verbatim string (cannot use $"", because of Cake version)
    string contents = string.Format(@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Cake.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

public partial class BuildParameters
{{
    public static BuildParameters GetParameters(
        ICakeContext context,
        BuildSystem buildSystem,
        BuildSettings settings,
        BuildPathSettings pathSettings = null
        )
    {{
        context.Information(""Maxfire.CakeScripts version {{0}} is being executed by this build."", CakeScriptsVersion);
        return GetParametersHelper(context, buildSystem, settings, pathSettings);
    }}

    public string CakeScriptsVersion
    {{
        get {{ return ""{0}""; }}
    }}
}}", parameters.VersionInfo.SemVer);

    var path = parameters.Paths.Directories.Src.CombineWithFilePath("CakeScriptsVersion.cake");
    System.IO.File.WriteAllText(path.FullPath, contents, Encoding.UTF8);
});

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(parameters.Target);
