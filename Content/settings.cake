// TODO: Solution.sln
// TODO: Mandatory fields: ProjectName, RepositoryName

public class BuildSettings
{
    // default values
    const string GITHUB_REPOSITORY_OWNER = "maxild";

    // TODO: GithubSettings
    private string _repositoryOwner;
    public string RepositoryOwner
    {
        get { return _repositoryOwner ?? GITHUB_REPOSITORY_OWNER; }
        set { _repositoryOwner = value; }
    }

    public string RepositoryName { get; set;}
    public string Remote { get { return string.Concat(RepositoryOwner, "/", RepositoryName); } }

    // TODO: EnvironmentSettings
    private EnvironmentVariableNames _environmentVariableNames;
    public EnvironmentVariableNames EnvironmentVariableNames
    {
        get { return _environmentVariableNames ?? new EnvironmentVariableNames(); }
        set { _environmentVariableNames = value; }
    }

    public bool PrintAppVeyorEnvironmentVariables { get; set; }

    // TODO: DotNetSettings
    public bool UseSystemDotNetPath { get; set; }
    public string DotNetCliInstallScriptUrl { get; set; }
    public string DotNetCliBranch { get; set; }
    public string DotNetCliChannel { get; set; }
    public string DotNetCliVersion { get; set; }
}

