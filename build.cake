///////////////////////////////////////////////////////////////////////////////
// SCRIPTS (load all files to check compilation)
///////////////////////////////////////////////////////////////////////////////
#load "src/failurehelpers.cake"
#load "src/githubrepository.cake"
#load "src/gitrepoinfo.cake"
#load "src/gitversioninfo.cake"
#load "src/parameters.cake"
#load "src/paths.cake"
#load "src/runhelpers.cake"
#load "src/settings.cake"
#load "src/toolrunner.cake"
#load "src/utils.cake"

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

// Here we call the (pseudo) private __GetParametersHelper__ method,
// because the way we generate the CakeScripts.Version property in the
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

    Information("Building version {0} of {1} ({2}, {3}) using version {4} of Cake and version {5} of GitVersion. (IsTagPush: {6})",
        parameters.VersionInfo.SemVer,
        parameters.ProjectName,
        parameters.Configuration,
        parameters.Target,
        parameters.VersionInfo.CakeVersion,
        parameters.VersionInfo.GitVersionVersion,
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

Task("Travis")
    .IsDependentOn("Info")
    .IsDependentOn("Package");

Task("AppVeyor")
    .IsDependentOn("Info")
    .IsDependentOn("Print-AppVeyor-Environment-Variables")
    .IsDependentOn("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Publish")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of " + parameters.ProjectName + ".  All publishing tasks have been attempted.");
    }
});

Task("Version").Does(() => parameters.VersionInfo.PrintToLog());

Task("Info").Does(() => parameters.PrintToLog());

Task("ReleaseNotes").IsDependentOn("Create-Release-Notes");

Task("Clean").Does(() => parameters.ClearArtifacts());

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Generate-CakeScripts-Version-Source-File")
    .IsDependentOn("Generate-Version-Txt-File")
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

Task("Create-Release-Notes")
    .Does(() =>
{
    GitReleaseManagerCreate(parameters.GitHub.GetRequiredToken(),
                            parameters.GitHub.RepositoryOwner, parameters.GitHub.RepositoryName,
        new GitReleaseManagerCreateSettings
        {
            Milestone         = parameters.VersionInfo.Milestone,
            TargetCommitish   = "master"
        });
});

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

public static class CakeScripts
{{
    public static BuildParameters GetParameters(
        ICakeContext context,
        BuildSystem buildSystem,
        BuildSettings settings,
        BuildPathSettings pathSettings = null
        )
    {{
        context.Information(""Maxfire.CakeScripts version {{0}} is being executed by this build."", Version);
        return BuildParameters.__GetParametersHelper__(context, buildSystem, settings, pathSettings);
    }}

    public static string Version
    {{
        get {{ return ""{0}""; }}
    }}
}}
", parameters.VersionInfo.SemVer);

    var path = parameters.Paths.Directories.Src.CombineWithFilePath("main.cake");
    System.IO.File.WriteAllText(path.FullPath, contents, Encoding.UTF8);
});

Task("Generate-Version-Txt-File")
    .Does(() =>
{
    EnsureDirectoryExists(parameters.Paths.Directories.TempArtifacts);
    var path = parameters.Paths.Directories.TempArtifacts.CombineWithFilePath("version.txt");
    System.IO.File.WriteAllText(path.FullPath, parameters.VersionInfo.NuGetVersion, Encoding.UTF8);
});

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(parameters.Target);
