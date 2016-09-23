// Must be called after GitVersionInfo.Calculate!!!!!
public class GitRepoInfo
{
    // GitFlow branching naming conventions and conventional PR branch naming
    const string FeatureBranchRegex          = @"^features?[/-]";
    const string HotfixBranchRegex           = @"^hotfix(es)?[/-]";
    const string ReleaseCandidateBranchRegex = @"^releases?[/-]";
    const string DevelopBranchRegex          = @"^dev(elop)?$";
    const string MasterBranchRegex           = @"^master$";
    const string SupportBranchRegex          = @"^support[/-]";
    const string PullRequestBranchRegex      = @"(pull|pull\-requests|pr)[/-]";

    private readonly ICakeContext _context;

    private GitRepoInfo(ICakeContext context)
    {
        _context = context;
    }

    // git rev-parse --short HEAD
    public string Sha { get; private set;}       // full sha with 40 chars (a3497c9f044f45b5e295f7fb9d7494df3c209a31)

    // git rev-parse HEAD or git rev-parse --verify HEAD
    public string CommitId { get; private set; } // partial sha with 7 chars (a3497c9)

    // git rev-parse --abbrev-ref HEAD
    public string Branch { get; private set; }

    public bool IsFeatureBranch { get; private set; }
    public bool IsHotfixBranch { get; private set; }
    public bool IsReleaseCandidateBranch { get; private set; }
    public bool IsDevelopBranch { get; private set; }
    public bool IsMasterBranch { get; private set; }
    public bool IsSupportBranch { get; private set; }
    public bool IsPullRequestBranch { get; private set; }

    // git tag -l --points-at HEAD
    public string Tag { get; private set; }

    public bool IsTag { get { return false == string.IsNullOrEmpty(Tag); } }

    // git remote get-url origin
    public string RepositoryOwner { get; private set; }
    public string RepositoryName { get; private set; }

    public bool IsRemoteEqualToRepoSettings(BuildSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        bool sameOwner = RepositoryOwner.Equals(settings.RepositoryOwner, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(settings.RepositoryName))
        {
            return sameOwner;
        }

        bool sameName = RepositoryName.Equals(settings.RepositoryName, StringComparison.OrdinalIgnoreCase);

        return sameOwner && sameName;
    }

    public void PrintToLog()
    {
        _context.Information("GIT Repository Information:");
        _context.Information("  CommitId:                 {0}", CommitId);
        _context.Information("  Sha:                      {0}", Sha);
        _context.Information("  Branch:                   {0}", Branch);
        _context.Information("  IsFeatureBranch:          {0}", IsFeatureBranch);
        _context.Information("  IsHotfixBranch:           {0}", IsHotfixBranch);
        _context.Information("  IsReleaseCandidateBranch: {0}", IsReleaseCandidateBranch);
        _context.Information("  IsDevelopBranch:          {0}", IsDevelopBranch);
        _context.Information("  IsMasterBranch:           {0}", IsMasterBranch);
        _context.Information("  IsSupportBranch:          {0}", IsSupportBranch);
        _context.Information("  IsPullRequestBranch:      {0}", IsPullRequestBranch);
        _context.Information("  Tag:                      {0}", Tag);
        _context.Information("  IsTag:                    {0}", IsTag);
        _context.Information("  RepositoryOwner:          {0}", RepositoryOwner);
        _context.Information("  RepositoryName:           {0}", RepositoryName);
    }

    public static GitRepoInfo Calculate(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        // using env var PATH
        var git = new GitExec(context) ;

        context.Information("Git path: {0}", git.Path);

        string branch = git.Command("rev-parse --verify --abbrev-ref HEAD");

        // zero or one tag seem most likely, but a single commit can have many tags (we pick arbitrary tag, if many exists)
        string[] tags = git.Command("tag -l --points-at HEAD").Split(' ');
        string tag = tags.Length > 0 ? tags[0] : string.Empty;

        string remoteUrl = git.Command("remote get-url origin");  // https://github.com/maxild/CakeScripts.git
        string[] remoteUrlSegments = new Uri(remoteUrl).Segments; // [ "/", "maxild/", "CakeScripts.git" ]

        string repoOwner, repoName;
        if (remoteUrlSegments.Length != 3)
        {
            // TODO: Maybe throw exception
            context.Warning("Unable to resolve RepositoryOwner and RepositoryName from remote url '{0}'");
            repoOwner = string.Empty;
            repoName = string.Empty;
        }
        else
        {
            repoOwner = remoteUrlSegments[1].TrimEnd('/');
            repoName = StringUtils.TrimEnd(remoteUrlSegments[2], ".git");
        }

        return new GitRepoInfo(context)
        {
            Sha = git.Command("rev-parse --verify HEAD"),
            CommitId = git.Command("rev-parse --verify --short HEAD"),
            Branch = branch,
            IsFeatureBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, FeatureBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsDevelopBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, DevelopBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsHotfixBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, HotfixBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsReleaseCandidateBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, ReleaseCandidateBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsMasterBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, MasterBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsSupportBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, SupportBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsPullRequestBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, PullRequestBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            Tag = tag,
            RepositoryOwner = repoOwner,
            RepositoryName = repoName
        };
    }

    public class GitExec
    {
        private readonly ICakeContext _context;
        private readonly string _gitPath;

        public GitExec(ICakeContext context)
        {
            _context = context;
            var path = context.Tools.Resolve("git.exe").FullPath;
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Cake could not resolve the PATH to git.exe");
            }
            Path = path;
        }

        public string Path { get; private set; }

        // TODO: Move to runhelpers.cake (where stdout capturing command is missing)
        public string Command(string arguments)
        {
            //_context.Information("git {0}", arguments);
            IEnumerable<string> stdout;
            int exit = _context.StartProcess(Path, new ProcessSettings {
                Arguments = arguments,
                RedirectStandardOutput = true,
            }, out stdout);
            string result = exit == 0 ? (string.Join(" ", stdout) ?? string.Empty).Trim() : string.Empty;
            //_context.Information("git {0} --> {1}", arguments, result);
            return result;
        }
    }
}
