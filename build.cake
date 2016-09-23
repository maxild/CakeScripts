// Load all scripts to check compilation
//#load "content/credentials.cake"
//#load "content/gitreleasemanager.cake"
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
        },
        PrintAppVeyorEnvironmentVariables = true
    },
    new BuildPathSettings
    {
        NuspecDir = "."
    });

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    if (parameters.Git.IsMasterBranch && (context.Log.Verbosity != Verbosity.Diagnostic)) {
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

Task("Print-Parameters")
    .Does(() =>
{
    parameters.PrintToLog();
});

Task("Default")
    .IsDependentOn("Print-Parameters");

RunTarget(parameters.Target);
