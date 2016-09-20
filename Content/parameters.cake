public class BuildParameters
{
    // GitFlow branching naming conventions
    const string FeatureBranchRegex = "^features?[/-]";
    const string HotfixBranchRegex = "^hotfix(es)?[/-]";
    const string ReleaseCandidateBranchRegex = "^releases?[/-]";
    const string DevelopBranchRegex = "^dev(elop)?$";
    const string MasterBranchRegex = "^master$";
    const string SupportBranchRegex = "^support[/-]";

    public string Target { get; private set; }
    public string Configuration { get; private set; }

    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnAppVeyor { get; private set; }

    public bool IsRunningOnUnix { get; private set; }
    public bool IsRunningOnWindows { get; private set; }

    public bool IsPullRequest { get; private set; }
    public bool IsFeatureBranch { get; private set; }
    public bool IsHotfixBranch { get; private set; }
    public bool IsReleaseCandidateBranch { get; private set; }
    public bool IsDevelopBranch { get; private set; }
    public bool IsMasterBranch { get; private set; }
    public bool IsSupportBranch { get; private set; }

    public bool IsMainRepository { get; private set; } // not a fork

    public bool IsTagged { get; private set; }

    //public bool IsPublishBuild { get; private set; }
    //public bool IsReleaseBuild { get; private set; }

    public GitHubCredentials GitHub { get; private set; }
    public NuGetCredentials MyGet { get; private set; }
    public NuGetCredentials NuGet { get; private set; }
    public AppVeyorCredentials AppVeyor { get; private set; }

    public BuildVersion Version { get; private set; }
    //public BuildPaths Paths { get; private set; }

    public void SetBuildVersion(BuildVersion version)
    {
        Version  = version;
    }

    // public void SetBuildPaths(BuildPaths paths)
    // {
    //     Paths  = paths;
    // }

    public static BuildParameters GetParameters(
        ICakeContext context,
        BuildSystem buildSystem,
        string repositoryOwner,
        string repositoryName
        )
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        var target = context.Argument("target", "Default");
        var configuration = context.Argument("configuration", "Release");

        return new BuildParameters {
            Target = target,
            Configuration = configuration,
            IsLocalBuild = buildSystem.IsLocalBuild,
            IsRunningOnUnix = context.IsRunningOnUnix(),
            IsRunningOnWindows = context.IsRunningOnWindows(),
            IsRunningOnAppVeyor = buildSystem.AppVeyor.IsRunningOnAppVeyor,
            IsPullRequest = buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest,
            IsFeatureBranch = System.Text.RegularExpressions.Regex.IsMatch(buildSystem.AppVeyor.Environment.Repository.Branch, FeatureBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsDevelopBranch = System.Text.RegularExpressions.Regex.IsMatch(buildSystem.AppVeyor.Environment.Repository.Branch, DevelopBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsHotfixBranch = System.Text.RegularExpressions.Regex.IsMatch(buildSystem.AppVeyor.Environment.Repository.Branch, HotfixBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsReleaseCandidateBranch = System.Text.RegularExpressions.Regex.IsMatch(buildSystem.AppVeyor.Environment.Repository.Branch, ReleaseCandidateBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsMasterBranch = System.Text.RegularExpressions.Regex.IsMatch(buildSystem.AppVeyor.Environment.Repository.Branch, MasterBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsSupportBranch = System.Text.RegularExpressions.Regex.IsMatch(buildSystem.AppVeyor.Environment.Repository.Branch, SupportBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsTagged = (
                buildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
                !string.IsNullOrWhiteSpace(buildSystem.AppVeyor.Environment.Repository.Tag.Name)
            ),
            IsMainRepository = StringComparer.OrdinalIgnoreCase.Equals(string.Concat(repositoryOwner, "/", repositoryName), buildSystem.AppVeyor.Environment.Repository.Name),
            // IsPublishBuild = new [] {
            //     "Create-Release-Notes"
            // }.Any(
            //     releaseTarget => StringComparer.OrdinalIgnoreCase.Equals(releaseTarget, target)
            // ),
            // IsReleaseBuild = new [] {
            //     "Publish-NuGet-Packages",
            //     "Publish-Chocolatey-Packages",
            //     "Publish-GitHub-Release"
            // }.Any(
            //     publishTarget => StringComparer.OrdinalIgnoreCase.Equals(publishTarget, target)
            // )
            GitHub = GetGitHubCredentials(context),
            MyGet = GetMyGetCredentials(context),
            NuGet = GetNuGetCredentials(context),
            AppVeyor = GetAppVeyorCredentials(context)
        };
    }
}
