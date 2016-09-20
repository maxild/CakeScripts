public class GitHubCredentials
{
    public string UserName { get; private set; }
    public string Password { get; private set; }

    public GitHubCredentials(string userName, string password)
    {
        UserName = userName;
        Password = password;
    }
}

public class NuGetCredentials
{
    public string ApiKey { get; private set; }
    public string SourceUrl { get; private set; }

    public NuGetCredentials(string apiKey, string sourceUrl)
    {
        ApiKey = apiKey;
        SourceUrl = sourceUrl;
    }
}

public class AppVeyorCredentials
{
    public string ApiToken { get; private set; }

    public AppVeyorCredentials(string apiToken)
    {
        ApiToken = apiToken;
    }
}

public static GitHubCredentials GetGitHubCredentials(ICakeContext context)
{
    return new GitHubCredentials(
        context.EnvironmentVariable(githubUserNameVariable),
        context.EnvironmentVariable(githubPasswordVariable));
}

public static NuGetCredentials GetMyGetCredentials(ICakeContext context)
{
    return new NuGetCredentials(
        context.EnvironmentVariable(myGetApiKeyVariable),
        context.EnvironmentVariable(myGetSourceUrlVariable));
}

public static NuGetCredentials GetNuGetCredentials(ICakeContext context)
{
    return new NuGetCredentials(
        context.EnvironmentVariable(nuGetApiKeyVariable),
        context.EnvironmentVariable(nuGetSourceUrlVariable));
}

public static AppVeyorCredentials GetAppVeyorCredentials(ICakeContext context)
{
    return new AppVeyorCredentials(
        context.EnvironmentVariable(appVeyorApiTokenVariable));
}
