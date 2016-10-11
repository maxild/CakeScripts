// Wrapper around 'git remote get-url origin'
public class GitHubRepository
{
    // Git remote URLs (Named subexpr: (?<name>subexpression))
    //    https://github.com/USERNAME/REPOSITORY.git
    const string HttpsUrlPattern = @"^https:\/\/github.com\/(?<RepositoryOwner>\w+)/(?<RepositoryName>\w+)\.git$";
    //    git@github.com:USERNAME/REPOSITORY.git
    const string SshUrlPattern = @"^git@github.com:(?<RepositoryOwner>\w+)/(?<RepositoryName>\w+)\.git$";

    private readonly ICakeContext _context;

    public GitHubRepository(
        ICakeContext context,
        string owner,
        string name,
        bool hasHttpsUrl)
    {
        _context = context;
        Owner = owner;
        Name = name;
        HasHttpsUrl = hasHttpsUrl;
    }

    public string Owner { get; private set; }

    public string Name { get; private set; }

    public bool HasHttpsUrl { get; private set; }

    public string HttpsUrl
    {
        get { return string.Format("https://github.com/{0}/{1}.git", Owner, Name); }
    }

    public bool IsEqualToMainRepository(BuildSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        bool sameOwner = Owner.Equals(settings.MainRepositoryOwner, StringComparison.OrdinalIgnoreCase);

        // only compare if repo name have been configured in when
        // BuildParameters instance is constructed in global variables
        if (string.IsNullOrEmpty(settings.RepositoryName))
        {
            return sameOwner;
        }

        bool sameName = Name.Equals(settings.RepositoryName, StringComparison.OrdinalIgnoreCase);

        return sameOwner && sameName;
    }

        public void PrintToLog()
    {
        _context.Information("GitHub Repository Information:");
        _context.Information("  Owner:       {0}", Owner);
        _context.Information("  Name:        {0}", Name);
        _context.Information("  HasHttpsUrl: {0}", HasHttpsUrl);
    }

    public static GitHubRepository Calculate(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        // using env var PATH
        const char SEP = '#';
        var git = new GitExec(context, SEP);

        string remoteUrl = git.Command("remote get-url origin");  //

        string repoOwner, repoName;
        bool repoHasHttpsUrl = false;

        // AppVeyor uses https for public repos
        var httpsMatch = System.Text.RegularExpressions.Regex.Match(remoteUrl, HttpsUrlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (httpsMatch.Success)
        {
            repoOwner = httpsMatch.Groups["RepositoryOwner"].Value;
            repoName = httpsMatch.Groups["RepositoryName"].Value;
            repoHasHttpsUrl = true;
            context.Warning(@"RepositoryOwner '{0}' and RepositoryName '{1}' resolved from origin remote url of the form 'https://github.com/USERNAME/REPOSITORY.git'.", repoOwner, repoName);
        }
        else
        {
            // AppVeyor uses ssh for private repos
            var sshMatch = System.Text.RegularExpressions.Regex.Match(remoteUrl, SshUrlPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (sshMatch.Success) {
                repoOwner = httpsMatch.Groups["RepositoryOwner"].Value;
                repoName = httpsMatch.Groups["RepositoryName"].Value;
                context.Warning(@"RepositoryOwner '{0}' and RepositoryName '{1}' resolved from origin remote url of the form 'git@github.com:USERNAME/REPOSITORY.git'.", repoOwner, repoName);
            }
            else
            {
                // TODO: Maybe throw exception
                context.Warning("Unable to resolve RepositoryOwner and RepositoryName from remote url '{0}'", remoteUrl);
                repoOwner = string.Empty;
                repoName = string.Empty;
            }
        }

        return new GitHubRepository(
            context,
            name: repoName,
            owner: repoOwner,
            hasHttpsUrl: repoHasHttpsUrl);
    }
}