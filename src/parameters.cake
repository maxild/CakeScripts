public class BuildParameters
{
    private readonly ICakeContext _context;
    private readonly Func<BuildParameters, bool> _deployToCIFeedFunc;
    private readonly Func<BuildParameters, bool> _deployToProdFeedFunc;

    private BuildParameters(ICakeContext context, BuildSettings settings)
    {
        _context = context;
        _deployToCIFeedFunc = settings.DeployToCIFeed ?? DefaultDeployToCIFeed;
        _deployToProdFeedFunc = settings.DeployToProdFeed ?? DefaultDeployToProdFeed;
    }

    public string ProjectName { get; private set; }

    public string Target { get; private set; }
    public string Configuration { get; private set; }

    public bool ConfigurationIsDebug
    {
        get { return Configuration.Equals("Debug", StringComparison.OrdinalIgnoreCase); }
    }

    public bool ConfigurationIsRelease
    {
        get { return Configuration.Equals("Release", StringComparison.OrdinalIgnoreCase); }
    }

    public bool IsRunningOnUnix { get; private set; }
    public bool IsRunningOnWindows { get; private set; }

    public bool IsMainRepository { get; private set; } // not a fork?

    // AppVeyor variables
    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnAppVeyor { get; private set; }
    public bool IsPullRequest { get; private set; }
    public bool IsTagPush { get; private set; }

    public NuGetPushCredentials CIFeed { get; private set; }
    public NuGetPushCredentials ProdFeed { get; private set; }

    public bool ShouldDeployToCIFeed { get { return DeployToAnyFeed(this) && _deployToCIFeedFunc(this); } }
    public bool ShouldDeployToProdFeed { get { return DeployToAnyFeed(this) && _deployToProdFeedFunc(this); } }

    static bool DeployToAnyFeed(BuildParameters parameters)
    {
        return false == parameters.IsLocalBuild &&
               false == parameters.IsPullRequest &&
               parameters.IsMainRepository;
    }

    static bool DefaultDeployToCIFeed(BuildParameters parameters)
    {
        // Only Debug builds are published to CI feed
        // Any branch except master and 'support/x.y' have been pushed to GitHub
        return parameters.ConfigurationIsDebug && (false == parameters.IsTagPush || false == parameters.Git.IsReleaseLineBranch);
    }

    static bool DefaultDeployToProdFeed(BuildParameters parameters)
    {
        // Only Release builds are published to production feed
        // A tag (i.e. a published github release) on either master or 'support/x.y' have been created on GitHub
        return parameters.ConfigurationIsRelease && parameters.IsTagPush && parameters.Git.IsReleaseLineBranch;
    }

    public GitHubRepositoryAndCredentials GitHub { get; private set; }

    public Credentials MyGet { get; private set; }

    public BuildPaths Paths { get; private set; }

    public GitVersionInfo VersionInfo { get; private set; }

    public GitRepoInfo Git { get; private set; }

    public Project SrcProject(string projectName)
    {
        var projectPath = Paths.Directories.Src.Combine(projectName);
        return new Project(_context, projectPath, Configuration);
    }

    public Project TestProject(string testProjectName)
    {
        var testProjectPath = Paths.Directories.Test.Combine(testProjectName);
        return new Project(_context, testProjectPath, Configuration);
    }

    public IEnumerable<FilePath> GetBuildArtifacts(IEnumerable<string> projectNames)
    {
        return GetBuildArtifacts(projectNames.ToArray());
    }

    // project directory and build artifact most have the same name (not considering the extension)
    public IEnumerable<FilePath> GetBuildArtifacts(params string[] projectNames)
    {
        // paths to all direct subdirs below ./src and ./test
        var projectPaths = System.IO.Directory.EnumerateDirectories(Paths.Directories.Src.FullPath, "*", System.IO.SearchOption.TopDirectoryOnly)
            .Concat(System.IO.Directory.EnumerateDirectories(Paths.Directories.Test.FullPath, "*", System.IO.SearchOption.TopDirectoryOnly));

        foreach (string projectName in projectNames)
        {
            string projectPath = projectPaths
                .Where(path => projectName.Equals(System.IO.Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
                .First();

            var artifactName = string.Concat(projectName, ".dll");
            var artifactPath = _context.Directory("bin") + _context.Directory(Configuration) + _context.File(artifactName);
            var buildArtifactPath = ((DirectoryPath) projectPath).CombineWithFilePath(artifactPath);

            yield return buildArtifactPath;
        }
    }

    public void ClearArtifacts()
    {
        ClearTempArtifacts();
        if (_context.DirectoryExists(Paths.Directories.Artifacts))
        {
            _context.DeleteDirectory(Paths.Directories.Artifacts, new DeleteDirectorySettings {
                Recursive = true,
                Force = true
            });
        }
    }

    public void ClearTempArtifacts()
    {
        if (_context.DirectoryExists(Paths.Directories.TempArtifacts))
        {
            _context.DeleteDirectory(Paths.Directories.TempArtifacts, new DeleteDirectorySettings {
                Recursive = true,
                Force = true
            });
        }
    }

    public ToolRunner GetTool(string toolName)
    {
        return new ToolRunner(_context, new [] {toolName});
    }

    public ToolRunner GetTool(params string[] toolNames)
    {
        return new ToolRunner(_context, toolNames);
    }

    public void PrintToLog()
    {
        _context.Information("Target:                 {0}", Target);
        _context.Information("Configuration:          {0}", Configuration);
        _context.Information("IsRunningOnWindows:     {0}", IsRunningOnWindows);
        _context.Information("IsRunningOnUnix:        {0}", IsRunningOnUnix);
        _context.Information("IsMainRepository:       {0}", IsMainRepository);
        _context.Information("IsLocalBuild:           {0}", IsLocalBuild);
        _context.Information("IsRunningOnAppVeyor:    {0}", IsRunningOnAppVeyor);
        _context.Information("IsPullRequest:          {0}", IsPullRequest);
        _context.Information("IsTagPush:              {0}", IsTagPush);
        _context.Information("MyGet.UserName:         {0}", MyGet.UserName);
        _context.Information("CIFeed:                 {0}", CIFeed.SourceUrl);
        _context.Information("ShouldDeployToCIFeed:   {0}", ShouldDeployToCIFeed);
        _context.Information("ProdFeed:               {0}", ProdFeed.SourceUrl);
        _context.Information("ShouldDeployToProdFeed: {0}", ShouldDeployToProdFeed);

        GitHub.PrintToLog();
        Git.PrintToLog();
        VersionInfo.PrintToLog();
        Paths.PrintToLog();
    }

    public void PrintAppVeyorEnvironmentVariables()
    {
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

    // This method is used by public GetParameters in generated file
    public static BuildParameters __GetParametersHelper__(
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

        var gitHubCredentials = new Credentials(
            userName: settings.GitHubUserName ?? context.EnvironmentVariable(settings.GitHubUserNameVariable) ?? settings.MainRepositoryOwner,
            password: context.EnvironmentVariable(settings.GitHubPasswordVariable),
            token: context.EnvironmentVariable(settings.GitHubTokenVariable)
        );

        // Resolve repositoryOwner, repositoryName and whether or not remote url is https:// based
        var gitHubRepository = GitHubRepository.Calculate(context);

        // GitVersion will patch current branch (HEAD) on appveyor, therefore
        // we have to execute the gitVersion tool before....
        var versionInfo = GitVersionInfo.Calculate(context, buildSystem, gitHubCredentials, gitHubRepository);
        // ... executing any git commands like 'git rev-parse HEAD'
        var repoInfo = GitRepoInfo.Calculate(context, settings);

        var configuration = context.Argument("configuration", "Release");

        var projectName = settings.ProjectName ??
                          settings.RepositoryName ??
                          gitHubRepository.Name;

        return new BuildParameters(context, settings)
        {
            ProjectName = projectName,

            Target = context.Argument("target", "Default"),
            Configuration = configuration,

            IsRunningOnUnix = context.IsRunningOnUnix(),
            IsRunningOnWindows = context.IsRunningOnWindows(),

            IsMainRepository = gitHubRepository.IsEqualToMainRepository(settings),

            // Build system...
            IsLocalBuild = buildSystem.IsLocalBuild,
            // AppVeyor build system wraps appveyor env vars
            IsRunningOnAppVeyor = buildSystem.AppVeyor.IsRunningOnAppVeyor,
            IsPullRequest = buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest,
            IsTagPush = (
                buildSystem.AppVeyor.Environment.Repository.Tag.IsTag &&
                !string.IsNullOrWhiteSpace(buildSystem.AppVeyor.Environment.Repository.Tag.Name)
            ),

            CIFeed = new NuGetPushCredentials(
                sourceUrl: settings.DeployToCIFeedUrl ?? context.EnvironmentVariable(settings.DeployToCIFeedUrlVariable),
                apiKey: context.EnvironmentVariable(settings.DeployToCIApiKeyVariable) // secret
            ),
            ProdFeed = new NuGetPushCredentials(
                sourceUrl: settings.DeployToProdFeedUrl ?? context.EnvironmentVariable(settings.DeployToProdFeedUrlVariable),
                apiKey: context.EnvironmentVariable(settings.DeployToProdApiKeyVariable) // secret
            ),

            GitHub = new GitHubRepositoryAndCredentials(
                context,
                repo: gitHubRepository,
                main: gitHubRepository.IsGitRepository
                    ? new GitHubRepository(
                        context,
                        isGitRepository: true,
                        isGithubRepository: true,
                        owner: settings.MainRepositoryOwner,
                        name: settings.RepositoryName,
                        hasHttpsUrl: true)
                    : gitHubRepository,
                credentials: gitHubCredentials
            ),

            MyGet = new Credentials(
                userName: settings.MyGetUserName ?? context.EnvironmentVariable(settings.MyGetUserNameVariable) ?? "maxfire",
                password: context.EnvironmentVariable(settings.MyGetPasswordVariable),
                token: null
            ),

            VersionInfo = versionInfo,
            Git = repoInfo,
            Paths = new BuildPaths(context,
                                   settings,
                                   pathSettings ?? new BuildPathSettings(),
                                   projectName)
        };
    }

    public class Credentials
    {
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string Token { get; private set; }

        public string GetRequiredPassword()
        {
            if (string.IsNullOrEmpty(Password))
            {
                throw new InvalidOperationException("Could not resolve password.");
            }
            return Password;
        }

        public string GetRequiredToken()
        {
            if (string.IsNullOrEmpty(Token))
            {
                throw new InvalidOperationException("Could not resolve token.");
            }
            return Token;
        }

        public Credentials(string userName, string password, string token)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentException("UserName cannot be null or empty.");
            }
            UserName = userName;
            Password = password; // empty, if no environment variable is configured in appveyor
            Token = token;       // empty, if no environment variable is configured in appveyor
        }
    }

    public class NuGetPushCredentials
    {
        public string SourceUrl { get; private set; }
        public string ApiKey { get; private set; }

        public string GetRequiredSourceUrl()
        {
            if (string.IsNullOrEmpty(SourceUrl))
            {
                throw new InvalidOperationException("Could not resolve NuGet push URL.");
            }
            return SourceUrl;
        }

        public string GetRequiredApiKey()
        {
            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new InvalidOperationException("Could not resolve NuGet push API key.");
            }
            return ApiKey;
        }

        public NuGetPushCredentials(string sourceUrl, string apiKey)
        {
            SourceUrl = sourceUrl;
            ApiKey = apiKey;
        }
    }

    public class GitHubRepositoryAndCredentials
    {
        private readonly GitHubRepository _repo;
        private readonly GitHubRepository _main;
        private readonly Credentials _credentials;
        private readonly ICakeContext _context;

        public GitHubRepositoryAndCredentials(
            ICakeContext context,
            GitHubRepository repo,
            GitHubRepository main,
            Credentials credentials)
        {
            _context = context;
            _repo = repo;
            _main = main;
            _credentials = credentials;
        }

        public string UserName { get { return _credentials.UserName; } }
        public string Password { get { return _credentials.Password; } }
        public string Token { get { return _credentials.Token; } }

        public string MainRepositoryOwner { get { return _main.Owner; } }
        public string MainRepositoryName { get { return _main.Name; } }
        public string RepositoryOwner { get { return _repo.Owner; } }
        public string RepositoryName { get { return _repo.Name; } }

        public bool RepositoryHasHttpsUrl { get { return _repo.HasHttpsUrl; } }
        public string RepositoryHttpsUrl { get { return _repo.HttpsUrl;} }

        public string GetRequiredPassword()
        {
            return _credentials.GetRequiredPassword();
        }

        public string GetRequiredToken()
        {
            return _credentials.GetRequiredToken();
        }

        public void PrintToLog()
        {
            _context.Information("GitHub Information:");
            _context.Information("  UserName:              {0}", UserName);
            _context.Information("  MainRepositoryOwner:   {0}", MainRepositoryOwner);
            _context.Information("  MainRepositoryName:    {0}", MainRepositoryName);
            _context.Information("  RepositoryOwner:       {0}", RepositoryOwner);
            _context.Information("  RepositoryName:        {0}", RepositoryName);
            _context.Information("  RepositoryHasHttpsUrl: {0}", RepositoryHasHttpsUrl);
            _context.Information("  RepositoryHttpsUrl:    {0}", RepositoryHttpsUrl);
        }
    }
}

public class Project
{
    private readonly ICakeContext _context;
    private readonly string _configuration;

    public Project(ICakeContext context, DirectoryPath projectPath, string configuration)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        _context = context;
        Path = projectPath;
        _configuration = configuration;
    }

    public DirectoryPath Path { get; }

    public FilePath GetBuildArtifact(string buildArtifact)
    {
        var artifactPath = _context.Directory("bin") + _context.Directory(_configuration) + _context.File(buildArtifact);
        return Path.CombineWithFilePath(artifactPath);
    }

    public PackageReference[] GetPackageRefs(string csprojFileName = null)
    {
        var csprojFileNameToUse = csprojFileName ?? string.Concat(Path.GetDirectoryName(), ".csproj");

        // Get an IFile (cake object) for the csproj file
        var csprojFile = _context.FileSystem.GetFile(Path.CombineWithFilePath(csprojFileNameToUse));

        System.Xml.Linq.XDocument document;
        using (var stream = csprojFile.OpenRead())
        {
            document = System.Xml.Linq.XDocument.Load(stream);
        }

        // if we are querying a pre-SDK csproj file, we need the namespace in the xname queries
        var ns = document.Root?.Name.Namespace; // XName = FQ-name
        var nsName = ns?.NamespaceName;         // namespace part of the fully qualified name

        var packageReferenceXName = GetXName("PackageReference", nsName);
        var includeXName =          GetXName("Include", nsName);
        var versionXName =          GetXName("Version", nsName);

        var packageReferences = document.Descendants(packageReferenceXName).Select(
                    x => new PackageReference(
                        name: x.Attribute("Include")?.Value ?? x.Element(includeXName)?.Value,
                        version: x.Attribute("Version")?.Value ?? x.Element(versionXName)?.Value
                    )).ToArray();

        return packageReferences;
    }

    public string GetPackageRefVersionOf(string dependency, string csprojFileName = null)
    {
        return GetPackageRefs(csprojFileName).Single(x => x.Name.Equals(dependency, StringComparison.OrdinalIgnoreCase)).Version;
    }

    private static System.Xml.Linq.XName GetXName(string localName, string @namespace)
    {
        return @namespace == null ? System.Xml.Linq.XName.Get(localName) : System.Xml.Linq.XName.Get(localName, @namespace);
    }
}

public class PackageReference
{
    public PackageReference(string name, string version)
    {
        Name = name;
        Version = version;
    }
    public string Name { get; }
    public string Version { get; }
}
