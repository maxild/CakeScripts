// Environment Variable names that can be overriden (options pattern)
public class EnvironmentVariableNames
{
    // default names
    private static string GITHUB_PASSWORD          = "GITHUB_PASSWORD";
    private static string MYGET_MAXFIRE_API_KEY    = "MYGET_MAXFIRE_API_KEY";
    private static string MYGET_MAXFIRE_CI_API_KEY = "MYGET_MAXFIRE_CI_API_KEY";
    private static string MYGET_BRF_API_KEY        = "MYGET_BRF_API_KEY";
    private static string MYGET_BRF_CI_API_KEY     = "MYGET_BRF_CI_API_KEY";
    private static string NUGET_API_KEY            = "NUGET_API_KEY";

    private string _gitHubPasswordVariable;
    public string GitHubPasswordVariable
    {
        get { return _gitHubPasswordVariable ?? GITHUB_PASSWORD; }
        set { _gitHubPasswordVariable = value; }
    }

    private string _myGetMaxfireApiKeyVariable;
    public string MyGetMaxfireApiKeyVariable
    {
        get { return _myGetMaxfireApiKeyVariable ?? MYGET_MAXFIRE_API_KEY; }
        set { _myGetMaxfireApiKeyVariable = value; }
    }

    private string _myGetMaxfireCiApiKeyVariable;
    public string MyGetMaxfireCiApiKeyVariable
    {
        get { return _myGetMaxfireCiApiKeyVariable ?? MYGET_MAXFIRE_CI_API_KEY; }
        set { _myGetMaxfireCiApiKeyVariable = value; }
    }

    private string _myGetBrfApiKeyVariable;
    public string MyGetBrfApiKeyVariable
    {
        get { return _myGetBrfApiKeyVariable ?? MYGET_BRF_API_KEY; }
        set { _myGetBrfApiKeyVariable = value; }
    }

    private string _myGetBrfCiApiKeyVariable;
    public string MyGetBrfCiApiKeyVariable
    {
        get { return _myGetBrfCiApiKeyVariable ?? MYGET_BRF_CI_API_KEY; }
        set { _myGetBrfCiApiKeyVariable = value; }
    }

    private string _nuGetApiKeyVariable;
    public string NuGetApiKeyVariable
    {
        get { return _nuGetApiKeyVariable ?? NUGET_API_KEY; }
        set { _nuGetApiKeyVariable = value; }
    }
}
