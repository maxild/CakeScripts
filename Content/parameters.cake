public class BuildParameters
{
    private readonly ICakeContext _context;
    private readonly BuildSettings _settings;

    private BuildParameters(ICakeContext context, BuildSettings settings)
    {
        _context = context;
        _settings = settings;
    }

    public string Target { get; private set; }
    public string Configuration { get; private set; }

    public bool IsRunningOnUnix { get; private set; }
    public bool IsRunningOnWindows { get; private set; }

    public bool IsMainRepository { get; private set; } // not a fork?
    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnAppVeyor { get; private set; }
    public bool IsPullRequest { get; private set; }
    public bool IsTagPush { get; private set; }

    //public bool IsPublishBuild { get; private set; }
    //public bool IsReleaseBuild { get; private set; }

    // public GitHubCredentials GitHub { get; private set; }
    // public NuGetCredentials MyGet { get; private set; }
    // public NuGetCredentials NuGet { get; private set; }

    public ProjectInfo Project { get; private set; }

    public SecretEnvironment Secrets { get; private set; }

    public BuildPaths Paths { get; private set; }

    public GitVersionInfo VersionInfo { get; private set; }

    public GitRepoInfo Git { get; private set; }

    public void PrintToLog()
    {
        _context.Information("Target:              {0}", Target);
        _context.Information("Configuration:       {0}", Configuration);
        _context.Information("IsRunningOnWindows:  {0}", IsRunningOnWindows);
        _context.Information("IsRunningOnUnix:     {0}", IsRunningOnUnix);
        _context.Information("IsMainRepository:    {0}", IsMainRepository);
        _context.Information("IsLocalBuild:        {0}", IsLocalBuild);
        _context.Information("IsRunningOnAppVeyor: {0}", IsRunningOnAppVeyor);
        _context.Information("IsPullRequest:       {0}", IsPullRequest);
        _context.Information("IsTagPush:           {0}", IsTagPush);
        VersionInfo.PrintToLog();
        Git.PrintToLog();
        Paths.PrintToLog();
        if (_settings.PrintAppVeyorEnvironmentVariables)
        {
            PrintAppVeyorEnvironmentVariables();
        }
    }

    public void PrintAppVeyorEnvironmentVariables()
    {
        // TODO: Maybe verbose here???
        if (IsRunningOnAppVeyor)
        {
            _context.Information("CI:                                    {0}", EnvironmentVariable("CI"));
            _context.Information("APPVEYOR_API_URL:                      {0}", EnvironmentVariable("APPVEYOR_API_URL"));
            _context.Information("APPVEYOR_ACCOUNT_NAME :                {0}", EnvironmentVariable("APPVEYOR_ACCOUNT_NAME"));
            _context.Information("APPVEYOR_PROJECT_ID:                   {0}", EnvironmentVariable("APPVEYOR_PROJECT_ID"));
            _context.Information("APPVEYOR_PROJECT_NAME:                 {0}", EnvironmentVariable("APPVEYOR_PROJECT_NAME"));
            _context.Information("APPVEYOR_PROJECT_SLUG:                 {0}", EnvironmentVariable("APPVEYOR_PROJECT_SLUG"));
            _context.Information("APPVEYOR_BUILD_FOLDER:                 {0}", EnvironmentVariable("APPVEYOR_BUILD_FOLDER"));
            _context.Information("APPVEYOR_BUILD_ID:                     {0}", EnvironmentVariable("APPVEYOR_BUILD_ID"));
            _context.Information("APPVEYOR_BUILD_NUMBER:                 {0}", EnvironmentVariable("APPVEYOR_BUILD_NUMBER"));
            _context.Information("APPVEYOR_BUILD_VERSION:                {0}", EnvironmentVariable("APPVEYOR_BUILD_VERSION"));
            _context.Information("APPVEYOR_PULL_REQUEST_NUMBER:          {0}", EnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER"));
            _context.Information("APPVEYOR_PULL_REQUEST_TITLE:           {0}", EnvironmentVariable("APPVEYOR_PULL_REQUEST_TITLE"));
            _context.Information("APPVEYOR_JOB_ID:                       {0}", EnvironmentVariable("APPVEYOR_JOB_ID"));
            _context.Information("APPVEYOR_JOB_NAME:                     {0}", EnvironmentVariable("APPVEYOR_JOB_NAME"));
            _context.Information("APPVEYOR_REPO_PROVIDER:                {0}", EnvironmentVariable("APPVEYOR_REPO_PROVIDER"));
            _context.Information("APPVEYOR_REPO_SCM:                     {0}", EnvironmentVariable("APPVEYOR_REPO_SCM"));
            _context.Information("APPVEYOR_REPO_NAME:                    {0}", EnvironmentVariable("APPVEYOR_REPO_NAME"));
            _context.Information("APPVEYOR_REPO_BRANCH:                  {0}", EnvironmentVariable("APPVEYOR_REPO_BRANCH"));
            _context.Information("APPVEYOR_REPO_TAG:                     {0}", EnvironmentVariable("APPVEYOR_REPO_TAG"));
            _context.Information("APPVEYOR_REPO_TAG_NAME:                {0}", EnvironmentVariable("APPVEYOR_REPO_TAG_NAME"));
            _context.Information("APPVEYOR_REPO_COMMIT:                  {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT"));
            _context.Information("APPVEYOR_REPO_COMMIT_AUTHOR:           {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT_AUTHOR"));
            _context.Information("APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL:     {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT_AUTHOR_EMAIL"));
            _context.Information("APPVEYOR_REPO_COMMIT_TIMESTAMP:        {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT_TIMESTAMP"));
            _context.Information("APPVEYOR_REPO_COMMIT_MESSAGE:          {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE"));
            _context.Information("APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED: {0}", EnvironmentVariable("APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED"));
            _context.Information("APPVEYOR_SCHEDULED_BUILD:              {0}", EnvironmentVariable("APPVEYOR_SCHEDULED_BUILD"));
            _context.Information("APPVEYOR_FORCED_BUILD:                 {0}", EnvironmentVariable("APPVEYOR_FORCED_BUILD"));
            _context.Information("APPVEYOR_RE_BUILD:                     {0}", EnvironmentVariable("APPVEYOR_RE_BUILD"));
            _context.Information("PLATFORM:                              {0}", EnvironmentVariable("PLATFORM"));
            _context.Information("CONFIGURATION:                         {0}", EnvironmentVariable("CONFIGURATION"));
        }
    }

    string EnvironmentVariable(string variable)
    {
        return _context.EnvironmentVariable(variable);
    }

    public static BuildParameters GetParameters(
        ICakeContext context,
        BuildSystem buildSystem,
        BuildSettings settings,
        BuildPathSettings pathSettings = null
        )
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        if (buildSystem == null)
        {
            throw new ArgumentNullException("buildSystem");
        }
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        // GitVersion will patch current branch (HEAD) on appveyor, therefore
        // we have to execute the gitVersion tool before....
        var versionInfo = GitVersionInfo.Calculate(context, buildSystem);
        // ... executing any git commands like 'git rev-parse HEAD'
        var repoInfo = GitRepoInfo.Calculate(context);

        var projectInfo = new ProjectInfo(context, settings, repoInfo);

        return new BuildParameters(context, settings)
        {
            Target = context.Argument("target", "Default"),
            Configuration = context.Argument("configuration", "Release"),

            IsRunningOnUnix = context.IsRunningOnUnix(),
            IsRunningOnWindows = context.IsRunningOnWindows(),

            IsMainRepository = repoInfo.IsRemoteEqualToRepoSettings(settings),

            // Build system...
            IsLocalBuild = buildSystem.IsLocalBuild,
            // AppVeyor build system wraps appveyor env vars
            IsRunningOnAppVeyor = buildSystem.AppVeyor.IsRunningOnAppVeyor,
            IsPullRequest = buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest, // TODO: IsPullRequestBranch
            IsTagPush = (
                buildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
                !string.IsNullOrWhiteSpace(buildSystem.AppVeyor.Environment.Repository.Tag.Name)
            ),

            VersionInfo = versionInfo,
            Git = repoInfo,
            Secrets = new SecretEnvironment(context, settings.EnvironmentVariableNames),
            Project = projectInfo,
            Paths = new BuildPaths(context, settings, pathSettings ?? new BuildPathSettings(), projectInfo)
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

public class ProjectInfo
{
    private readonly ICakeContext _context;

    public ProjectInfo(ICakeContext context, BuildSettings settings, GitRepoInfo repoInfo)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        _context = context;

        Name = settings.ProjectName ??
                settings.RepositoryName ??
                repoInfo.RepositoryName ?? string.Empty;
    }

    public string Name { get; private set; }

    public void PrintToLog()
    {
        _context.Information("Name: {0}", Name);
    }
}

