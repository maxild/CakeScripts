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
        RepositoryName = "CakeScripts",
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
