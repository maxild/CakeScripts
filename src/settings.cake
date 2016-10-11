public class BuildSettings
{
    public string ProjectName { get; set; }

    // The following 2 properties are used to determine
    // if we are building the main repo or a fork

    private string _repositoryOwner;
    public string MainRepositoryOwner
    {
        get { return _repositoryOwner ?? "maxild"; }
        set { _repositoryOwner = value; }
    }

    public string RepositoryName { get; set; }

    // The following 4 settings can either be configured directly
    // (in build.cake) through the below setters or configured
    // (in appveyor.yml) through environment variables.
    public string GitHubUserName { get; set; }
    public string MyGetUserName  { get; set; }
    public string DeployToCIFeedUrl { get; set; }
    public string DeployToProdFeedUrl { get; set; }

    public Func<BuildParameters, bool> DeployToCIFeed { get; set; }
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
    const string MYGET_PASSWORD            = "MYGET_PASSWORD";
    const string MYGET_USERNAME            = "MYGET_USERNAME";

    const string CI_DEPLOYMENT_API_KEY        = "CI_DEPLOYMENT_API_KEY";
    const string CI_DEPLOYMENT_FEED_URL       = "CI_DEPLOYMENT_FEED_URL";
    const string PROD_DEPLOYMENT_API_KEY      = "DEPLOYMENT_API_KEY";
    const string PROD_DEPLOYMENT_FEED_URL     = "DEPLOYMENT_FEED_URL";

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

    private string _myGetUserNameVariable;
    public string MyGetUserNameVariable
    {
        get { return _myGetUserNameVariable ?? MYGET_USERNAME; }
        set { _myGetUserNameVariable = value; }
    }

    private string _myGetPasswordVariable;
    public string MyGetPasswordVariable
    {
        get { return _myGetPasswordVariable ?? MYGET_PASSWORD; }
        set { _myGetPasswordVariable = value; }
    }

    private string _cIDeploymentFeedUrlVariable;
    public string DeployToCIFeedUrlVariable
    {
        get { return _cIDeploymentFeedUrlVariable ?? CI_DEPLOYMENT_FEED_URL; }
        set { _cIDeploymentFeedUrlVariable = value; }
    }

    private string _cIDeploymentApiKeyVariable;
    public string DeployToCIApiKeyVariable
    {
        get { return _cIDeploymentApiKeyVariable ?? CI_DEPLOYMENT_API_KEY; }
        set { _cIDeploymentApiKeyVariable = value; }
    }

    private string _prodDeploymentFeedUrlVariable;
    public string DeployToProdFeedUrlVariable
    {
        get { return _prodDeploymentFeedUrlVariable ?? PROD_DEPLOYMENT_FEED_URL; }
        set { _prodDeploymentFeedUrlVariable = value; }
    }

    private string _prodDeploymentApiKeyVariable;
    public string DeployToProdApiKeyVariable
    {
        get { return _prodDeploymentApiKeyVariable ?? PROD_DEPLOYMENT_API_KEY; }
        set { _prodDeploymentApiKeyVariable = value; }
    }
}
