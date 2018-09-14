// Must be called after GitVersionInfo.Calculate!!!!!
public class GitRepoInfo
{
    // GitFlow branching naming conventions and conventional PR branch naming
    const string DevelopBranchRegex          = @"^dev(elop)?$";
    const string MasterBranchRegex           = @"^master$";
    const string FeatureBranchRegex          = @"^features?/";
    const string HotfixBranchRegex           = @"^hotfix(es)?/";
    const string ReleaseCandidateBranchRegex = @"^releases?/(0|[1-9]\d*)[.](0|[1-9]\d*)([.](0|[1-9]\d*))?"; // release/major.minor[.patch]
    const string SupportBranchRegex          = @"^support/(0|[1-9]\d*)[.](x|0|[1-9]\d*)";                   // support/1.2.x, support/1.x
    const string PullRequestBranchRegex      = @"^(pull|pull\-requests|pr)[/-]";

    private readonly ICakeContext _context;

    private GitRepoInfo(ICakeContext context)
    {
        _context = context;
    }

    // git rev-parse --short HEAD
    public string Sha { get; private set;}       // full sha with 40 chars (a3497c9f044f45b5e295f7fb9d7494df3c209a31)

    // git rev-parse HEAD or git rev-parse --verify HEAD
    public string CommitId { get; private set; } // partial sha with 7 chars (a3497c9)

    // git rev-list --format=%ad --date=iso-strict --max-count=1 HEAD
    public DateTimeOffset CommitDate { get; private set; } // Author date as strict ISO format with timezone offset

    // git rev-parse --abbrev-ref HEAD
    public string Branch { get; private set; }

    public bool IsFeatureBranch { get; private set; }
    public bool IsHotfixBranch { get; private set; }
    public bool IsReleaseCandidateBranch { get; private set; }
    public bool IsDevelopBranch { get; private set; }
    public bool IsMasterBranch { get; private set; }
    public bool IsSupportBranch { get; private set; }
    public bool IsPullRequestBranch { get; private set; }

    // GitFlow has 2 kind of release line branches with merge-commits from release/hotfix that are tagged.
    public bool IsReleaseLineBranch { get { return IsMasterBranch || IsSupportBranch; } }
    public bool IsDevelopmentLineBranch { get { return false == IsReleaseLineBranch; } }

    // git tag -l --points-at HEAD
    public string Tag { get; private set; }

    public bool IsTag { get { return false == string.IsNullOrEmpty(Tag); } }

    public void PrintToLog()
    {
        _context.Information("GIT Repository Information:");
        _context.Information("  CommitId:                 {0}", CommitId);
        _context.Information("  CommitDate:               {0}", CommitDate);
        _context.Information("  Sha:                      {0}", Sha);
        _context.Information("  Branch:                   {0}", Branch);
        _context.Information("  IsFeatureBranch:          {0}", IsFeatureBranch);
        _context.Information("  IsHotfixBranch:           {0}", IsHotfixBranch);
        _context.Information("  IsReleaseCandidateBranch: {0}", IsReleaseCandidateBranch);
        _context.Information("  IsDevelopBranch:          {0}", IsDevelopBranch);
        _context.Information("  IsMasterBranch:           {0}", IsMasterBranch);
        _context.Information("  IsSupportBranch:          {0}", IsSupportBranch);
        _context.Information("  IsReleaseLineBranch:      {0}", IsReleaseLineBranch);
        _context.Information("  IsPullRequestBranch:      {0}", IsPullRequestBranch);
        _context.Information("  Tag:                      {0}", Tag);
        _context.Information("  IsTag:                    {0}", IsTag);
    }

    public static GitRepoInfo Calculate(ICakeContext context, BuildSettings settings)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        var git = new ToolRunner(context, new [] {"git.exe", "git"});

        string isInsideWorkTree = git.Command("rev-parse --is-inside-work-tree");
        bool isInGitReposWorkingTree = "true".Equals(isInsideWorkTree, StringComparison.OrdinalIgnoreCase);

        if (false == isInGitReposWorkingTree)
        {
            return new GitRepoInfo(context)
            {
                Sha = "0000000000000000000000000000000000000000", // full sha with 40 chars
                CommitId = "0000000",
                CommitDate = new DateTimeOffset(),
                Branch = string.Empty,
                IsFeatureBranch = false,
                IsDevelopBranch = false,
                IsHotfixBranch = false,
                IsReleaseCandidateBranch = false,
                IsMasterBranch = false,
                IsSupportBranch = false,
                IsPullRequestBranch = false,
                Tag = string.Empty
            };
        }

        string branch = git.Command("rev-parse --verify --abbrev-ref HEAD");

        // 2016-09-26T14:59:32+02:00
        string commitDateAsIsoWithOffset = git
            .Command("rev-list --format=%ad --date=iso-strict --max-count=1 HEAD")
            .Split(new [] {git.NewLineToken}, StringSplitOptions.RemoveEmptyEntries)
            .Last();

        // zero or one tag seem most likely, but a single commit can have
        // many tags (we pick arbitrary tag, if many exists)
        string[] tags = git.Command("tag -l --points-at HEAD")
            .Split(new [] {git.NewLineToken}, StringSplitOptions.RemoveEmptyEntries);

        string tag = tags.Length > 0 ? tags[0] : string.Empty;

        return new GitRepoInfo(context)
        {
            Sha = git.Command("rev-parse --verify HEAD"),
            CommitId = git.Command("rev-parse --verify --short HEAD"),
            CommitDate = DateTimeOffset.ParseExact(commitDateAsIsoWithOffset, "yyyy-MM-dd'T'HH:mm:ss.FFFK", System.Globalization.CultureInfo.InvariantCulture),
            Branch = branch,
            IsFeatureBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, FeatureBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsDevelopBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, DevelopBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsHotfixBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, HotfixBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsReleaseCandidateBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, ReleaseCandidateBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsMasterBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, MasterBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsSupportBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, SupportBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            IsPullRequestBranch = System.Text.RegularExpressions.Regex.IsMatch(branch, PullRequestBranchRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            Tag = tag
        };
    }
}
