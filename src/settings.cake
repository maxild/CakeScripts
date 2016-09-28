public class BuildSettings
{
    public string ProjectName { get; set; }

    private string _repositoryOwner;
    public string RepositoryOwner
    {
        get { return _repositoryOwner ?? "maxild"; }
        set { _repositoryOwner = value; }
    }

    public string RepositoryName { get; set;}
    public string GitHubUserName { get; set; }

    public string DeployToCISourceUrl { get; set; }
    public string DeployToRCSourceUrl { get; set; }
    public string DeployToProdSourceUrl { get; set; }

    public Func<BuildParameters, bool> DeployToCIFeed { get; set; }
    public Func<BuildParameters, bool> DeployToRCFeed { get; set; }
    public Func<BuildParameters, bool> DeployToProdFeed { get; set; }

    public bool UseSystemDotNetPath { get; set; }
    public string DotNetCliInstallScriptUrl { get; set; }
    public string DotNetCliBranch { get; set; }
    public string DotNetCliChannel { get; set; }
    public string DotNetCliVersion { get; set; }

    //
    // Environment Variables
    //

    // default names
    const string GITHUB_PASSWORD           = "GITHUB_PASSWORD";
    const string GITHUB_USERNAME           = "GITHUB_USERNAME";
    const string CI_DEPLOYMENT_API_KEY     = "CI_DEPLOYMENT_API_KEY";
    const string CI_DEPLOYMENT_SOURCE_URL  = "CI_DEPLOYMENT_SOURCE_URL";
    const string RC_DEPLOYMENT_API_KEY     = "RC_DEPLOYMENT_API_KEY";
    const string RC_DEPLOYMENT_SOURCE_URL  = "RC_DEPLOYMENT_SOURCE_URL";
    const string DEPLOYMENT_API_KEY        = "DEPLOYMENT_API_KEY";
    const string DEPLOYMENT_SOURCE_URL     = "DEPLOYMENT_SOURCE_URL";

    private string _gitHubUserNameVariable;
    public string GitHubUserNameVariable
    {
        get { return _gitHubUserNameVariable ?? GITHUB_USERNAME; }
        set { _gitHubUserNameVariable = value; }
    }

    private string _gitHubPasswordVariable;
    public string GitHubPasswordVariable
    {
        get { return _gitHubPasswordVariable ?? GITHUB_PASSWORD; }
        set { _gitHubPasswordVariable = value; }
    }

    private string _cIDeploymentSourceUrlVariable;
    public string DeployToCISourceUrlVariable
    {
        get { return _cIDeploymentSourceUrlVariable ?? CI_DEPLOYMENT_SOURCE_URL; }
        set { _cIDeploymentSourceUrlVariable = value; }
    }

    private string _cIDeploymentApiKeyVariable;
    public string DeployToCIApiKeyVariable
    {
        get { return _cIDeploymentApiKeyVariable ?? CI_DEPLOYMENT_API_KEY; }
        set { _cIDeploymentApiKeyVariable = value; }
    }

    private string _rCDeploymentSourceUrlVariable;
    public string DeployToRCSourceUrlVariable
    {
        get { return _rCDeploymentSourceUrlVariable ?? RC_DEPLOYMENT_SOURCE_URL; }
        set { _rCDeploymentSourceUrlVariable = value; }
    }

    private string _rCDeploymentApiKeyVariable;
    public string DeployToRCApiKeyVariable
    {
        get { return _rCDeploymentApiKeyVariable ?? RC_DEPLOYMENT_API_KEY; }
        set { _rCDeploymentApiKeyVariable = value; }
    }

    private string _prodDeploymentSourceUrlVariable;
    public string DeployToProdSourceUrlVariable
    {
        get { return _prodDeploymentSourceUrlVariable ?? DEPLOYMENT_SOURCE_URL; }
        set { _prodDeploymentSourceUrlVariable = value; }
    }

    private string _prodDeploymentApiKeyVariable;
    public string DeployToProdApiKeyVariable
    {
        get { return _prodDeploymentApiKeyVariable ?? DEPLOYMENT_API_KEY; }
        set { _prodDeploymentApiKeyVariable = value; }
    }
}
