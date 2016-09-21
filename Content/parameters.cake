public class BuildParameters
{
    private readonly ICakeContext _context;

    private BuildParameters(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        _context = context;
    }

    public string Target { get; private set; }
    public string Configuration { get; private set; }

    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnAppVeyor { get; private set; }
    public bool IsRunningOnUnix { get; private set; }
    public bool IsRunningOnWindows { get; private set; }

    public GitContext Git { get; private set; }

    public bool IsMainRepository { get; private set; } // not a fork?
    public bool IsPullRequest { get; private set; }
    public bool IsTagPush { get; private set; }

    //public bool IsPublishBuild { get; private set; }
    //public bool IsReleaseBuild { get; private set; }

    // public GitHubCredentials GitHub { get; private set; }
    // public NuGetCredentials MyGet { get; private set; }
    // public NuGetCredentials NuGet { get; private set; }

    public SecretEnvironment Secrets { get; private set; }

    public void PrintToLog()
    {
        _context.Information("Target:              {0}", Target);
        _context.Information("Configuration:       {0}", Configuration);
        _context.Information("IsLocalBuild:        {0}", IsLocalBuild);
        _context.Information("IsRunningOnAppVeyor: {0}", IsRunningOnAppVeyor);
        _context.Information("IsRunningOnWindows:  {0}", IsRunningOnWindows);
        _context.Information("IsRunningOnUnix:     {0}", IsRunningOnUnix);
        _context.Information("IsMainRepository:    {0}", IsMainRepository);
        _context.Information("IsPullRequest:       {0}", IsPullRequest);
        _context.Information("IsTagPush:           {0}", IsTagPush);
        Git.PrintToLog();
    }

    public static BuildParameters GetParameters(
        ICakeContext context,
        BuildSystem buildSystem,
        BuildSettings buildSettings
        )
    {
        // TODO: using AppVeyor is not very clever, because it only works on appveyor
        context.Information("Branch: {0}", buildSystem.AppVeyor.Environment.Repository.Branch);
        context.Information("IsTag: {0}", buildSystem.AppVeyor.Environment.Repository.Tag.IsTag);
        context.Information("TagName: {0}", buildSystem.AppVeyor.Environment.Repository.Tag.Name);
        context.Information("RepositoryName: {0}", buildSystem.AppVeyor.Environment.Repository.Name);

        var gitContext = GitContext.GetGitContext(context);

        return new BuildParameters(context) {
            Target = context.Argument("target", "Default"),
            Configuration = context.Argument("configuration", "Release"),
            IsLocalBuild = buildSystem.IsLocalBuild,
            IsRunningOnUnix = context.IsRunningOnUnix(),
            IsRunningOnWindows = context.IsRunningOnWindows(),
            Git = gitContext,
            IsMainRepository = StringComparer.OrdinalIgnoreCase.Equals(buildSettings.RepositoryId, gitContext.RepositoryId),

            // ApVeyor stuff...
            IsRunningOnAppVeyor = buildSystem.AppVeyor.IsRunningOnAppVeyor,
            IsPullRequest = buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest, // TODO: IsPullRequestBranch
            IsTagPush = ( // TODO: Investigate AppVeyor in cake.core
                buildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
                !string.IsNullOrWhiteSpace(buildSystem.AppVeyor.Environment.Repository.Tag.Name)
            ),

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
            Secrets = new SecretEnvironment(context, buildSettings.EnvironmentVariableNames),
            // TODO
            // GitHub = GetGitHubCredentials(context),
            // MyGet = GetMyGetCredentials(context),
            // NuGet = GetNuGetCredentials(context)
        };
    }

    public class SecretEnvironment
    {
        private readonly ICakeContext _context;
        private readonly EnvironmentVariableNames _nameProvider;

        public SecretEnvironment(ICakeContext context, EnvironmentVariableNames environmentVariableNames)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (environmentVariableNames == null)
            {
                throw new ArgumentNullException("environmentVariableNames");
            }
            _context = context;
            _nameProvider = environmentVariableNames;
        }

        public string GithubPassword { get { return _context.EnvironmentVariable(_nameProvider.GitHubPasswordVariable); } }

        public string MyGetMaxfireApiKey { get { return _context.EnvironmentVariable(_nameProvider.MyGetMaxfireApiKeyVariable); } }
        public string MyGetMaxfireCiApiKey { get { return _context.EnvironmentVariable(_nameProvider.MyGetMaxfireCiApiKeyVariable); } }

        public string MyGetBrfApiKey { get { return _context.EnvironmentVariable(_nameProvider.MyGetBrfApiKeyVariable); } }
        public string MyGetBrfCiApiKey { get { return _context.EnvironmentVariable(_nameProvider.MyGetBrfCiApiKeyVariable); } }

        public string NuGetApiKey { get { return _context.EnvironmentVariable(_nameProvider.NuGetApiKeyVariable); } }
    }

}
