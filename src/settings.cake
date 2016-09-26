// TODO: No default values in settings (user specified) DTO's
public class BuildSettings
{
    // default values
    const string GITHUB_REPOSITORY_OWNER = "maxild";

    public string ProjectName { get; set; }

    // TODO: GithubSettings
    private string _repositoryOwner;
    public string RepositoryOwner
    {
        get { return _repositoryOwner ?? GITHUB_REPOSITORY_OWNER; }
        set { _repositoryOwner = value; }
    }

    public string RepositoryName { get; set;}

    // TODO: EnvironmentSettings
    private EnvironmentVariableNames _environmentVariableNames;
    public EnvironmentVariableNames EnvironmentVariableNames
    {
        get { return _environmentVariableNames ?? new EnvironmentVariableNames(); }
        set { _environmentVariableNames = value; }
    }

    // TODO: DotNetSettings
    public bool UseSystemDotNetPath { get; set; }
    public string DotNetCliInstallScriptUrl { get; set; }
    public string DotNetCliBranch { get; set; }
    public string DotNetCliChannel { get; set; }
    public string DotNetCliVersion { get; set; }
}