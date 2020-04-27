#tool nuget:?package=GitVersion.CommandLine&version=5.2.4

public class GitVersionInfo
{
    private readonly ICakeContext _context;

    private GitVersionInfo(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        _context = context;
    }

    public string MajorMinorPatch { get; private set; }

    // --version-suffix on dotnet-pack
    public string VersionSuffix { get; private set; }

    public bool IsPrerelease { get { return false == string.IsNullOrEmpty(VersionSuffix); }}

    public string NuGetVersion {
        get
        {
            if (false == IsPrerelease)
            {
                // official version
                return MajorMinorPatch;
            }
            // pre-release version
            return string.Concat(MajorMinorPatch, "-", VersionSuffix);
        }
    }

    public string AssemblyVersion { get; private set; }
    public string AssemblyFileVersion { get; private set; }
    public string AssemblyInformationalVersion { get; private set; }

    public string SemVer { get; private set; }

    public string Milestone { get { return MajorMinorPatch; } }

    public string CakeVersion { get; private set; }

    public string BuildVersion { get; private set; }

    public void PrintToLog()
    {
        _context.Information("Version Information:");
        _context.Information("  MajorMinorPatch:              {0}", MajorMinorPatch);
        _context.Information("  VersionSuffix:                {0}", VersionSuffix);
        _context.Information("  NuGetVersion:                 {0}", NuGetVersion);
        _context.Information("  IsPrerelease:                 {0}", IsPrerelease);
        _context.Information("  AssemblyVersion:              {0}", AssemblyVersion);
        _context.Information("  AssemblyFileVersion:          {0}", AssemblyFileVersion);
        _context.Information("  AssemblyInformationalVersion: {0}", AssemblyInformationalVersion);
        _context.Information("  SemVer:                       {0}", SemVer);
        _context.Information("  Milestone:                    {0}", Milestone);
        _context.Information("  BuildVersion:                 {0}", BuildVersion);
    }

    public static GitVersionInfo Calculate(
        ICakeContext context,
        BuildSystem buildSystem,
        BuildParameters.Credentials gitHubCredentials,
        GitHubRepository gitHubRepository
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
        if (gitHubCredentials == null)
        {
            throw new ArgumentNullException("gitHubCredentials");
        }
        if (gitHubRepository == null)
        {
            throw new ArgumentNullException("gitHubRepository");
        }

        // local function
        string EnvironmentVariable(string variable)
        {
            return context.EnvironmentVariable(variable);
        }

        string majorMinorPatch, pkgVersion, semVer, infoVer;

        if (gitHubRepository.IsGitRepository)
        {
            // TODO: GitVersion running on Unix (.NET Core)
            if (context.IsRunningOnWindows())
            {
                context.Information("Calculating Semantic Version...");

                // In case the GitHub repository requires authentication (i.e
                // is a private repository) we configure GitHub credentials
                // such that libgit2sharp can be configured via env vars
                IDictionary<string, string> environmentVariables = null;

                if (false == string.IsNullOrEmpty(gitHubCredentials.UserName)) {
                    context.Information("GitHub UserName '{0}' was found.", gitHubCredentials.UserName);

                    if (false == string.IsNullOrEmpty(gitHubCredentials.Token))
                    {
                        context.Information("Environment variable GITHUB_ACCESS_TOKEN was found.");
                        environmentVariables = new Dictionary<string, string>
                        {
                            { "GITVERSION_REMOTE_USERNAME", gitHubCredentials.UserName },
                            { "GITVERSION_REMOTE_PASSWORD", gitHubCredentials.Token }
                        };
                    }
                    else if (false == string.IsNullOrEmpty(gitHubCredentials.Password ))
                    {
                        context.Information("Environment variable GITHUB_PASSWORD was found.");
                        environmentVariables = new Dictionary<string, string>
                        {
                            { "GITVERSION_REMOTE_USERNAME", gitHubCredentials.UserName },
                            { "GITVERSION_REMOTE_PASSWORD", gitHubCredentials.Password }
                        };
                    }
                    else {
                        context.Information("Environment variables GITHUB_PASSWORD or GITHUB_ACCESS_TOKEN cannot be found.");
                    }
                }
                else {
                    throw new InvalidOperationException("UNEXPECTED: No GitHub UserName can be found.");
                }

                if (false == buildSystem.IsLocalBuild)
                {
                    // Running on AppVeyor, we have to patch private repos
                    //   GitVersion (i.e. libgit2) doesn't support SSH, so to avoid 'Unsupported URL protocol'
                    //   when GitVersion fetches from the remote, we have to convert to using HTTPS
                    //   See http://help.appveyor.com/discussions/kb/17-getting-gitversion-to-work-with-private-bitbucketgithub-repositories

                    if (false == gitHubRepository.HasHttpsUrl)
                    {
                        new ToolRunner(context, new [] {"git.exe", "git"})
                            .Command(string.Format("remote set-url origin {0}",
                                gitHubRepository.HttpsUrl));
                    }

                    // Running on AppVeyor, we have to patch/setup local tracking branches
                    context.GitVersion(new GitVersionSettings
                    {
                        OutputType = GitVersionOutput.BuildServer,
                        EnvironmentVariables = environmentVariables,
                        //NoFetch = true,
                        //ArgumentCustomization = args => args.Append("/nonormalize")
                    });

                    majorMinorPatch = EnvironmentVariable("GitVersion_MajorMinorPatch");
                    pkgVersion = EnvironmentVariable("GitVersion_LegacySemVerPadded");
                    semVer = EnvironmentVariable("GitVersion_SemVer");
                    infoVer = EnvironmentVariable("GitVersion_InformationalVersion");
                }

                var assertedVersions = context.GitVersion(new GitVersionSettings
                {
                    OutputType = GitVersionOutput.Json,
                    EnvironmentVariables = environmentVariables,
                    //NoFetch = true,
                    //ArgumentCustomization = args => args.Append("/nonormalize")
                });

                majorMinorPatch = assertedVersions.MajorMinorPatch;
                pkgVersion = assertedVersions.LegacySemVerPadded;
                semVer = assertedVersions.SemVer;
                infoVer = assertedVersions.InformationalVersion;

                context.Information("Calculated Semantic Version: {0}", semVer);
            }
            else
            {
                majorMinorPatch = "0.0.0";
                pkgVersion = string.Concat(majorMinorPatch, "-not-gitrepo");
                semVer = string.Concat(majorMinorPatch, "-not.gitrepo");
                infoVer = string.Concat(semVer, "+GitVersion.Was.Not.Called");
            }
        }
        else
        {
            majorMinorPatch = "0.0.0";
            pkgVersion = string.Concat(majorMinorPatch, "-not-windows");
            semVer = string.Concat(majorMinorPatch, "-not.windows");
            infoVer = string.Concat(semVer, "+GitVersion.Was.Not.Called");
        }

        string cakeAssemblyVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();
        string cakeVersion = StringUtils.TrimEnd(cakeAssemblyVersion, ".0");

        // AppVeyor build version (Update-AppveyorBuild -Version $buildVersion)
        string buildVersion = "local";
        if (buildSystem.AppVeyor.IsRunningOnAppVeyor)
        {
            var apiUrl = EnvironmentVariable("APPVEYOR_API_URL") + "api/build";
            string buildNumber = EnvironmentVariable("APPVEYOR_BUILD_NUMBER");
            // buildVersion must be unique, otherwise request to appveyor fails
            buildVersion = $"{semVer}.build.{buildNumber}"; // we could use fullSemVer, but semVer seems OK
            var statusCode = UpdateAppveyorBuildVersion(apiUrl, buildVersion);
            if (statusCode != System.Net.HttpStatusCode.OK)
            {
                context.Warning("UpdateAppveyorBuildVersion: Request failed. Received status {0}", statusCode);
            }
        }

        return new GitVersionInfo(context)
        {
            MajorMinorPatch = majorMinorPatch,
            VersionSuffix = pkgVersion.Substring(majorMinorPatch.Length).TrimStart('-'),
            AssemblyVersion = majorMinorPatch,
            AssemblyFileVersion = string.Concat(majorMinorPatch, ".0"),
            AssemblyInformationalVersion = infoVer,
            SemVer = semVer,
            CakeVersion = cakeVersion,
            BuildVersion = buildVersion
        };
    }

    private static System.Net.HttpStatusCode UpdateAppveyorBuildVersion(string apiUrl, string buildVersion)
    {
        var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(apiUrl);
        request.Method = "PUT";

        var data = $"{{ \"version\": \"{buildVersion}\" }}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(data);
        request.ContentLength = bytes.Length;
        request.ContentType = "application/json";

        using (var writeStream = request.GetRequestStream())
        {
            writeStream.Write(bytes, 0, bytes.Length);
        }

        using (var response = (System.Net.HttpWebResponse)request.GetResponse())
        {
            return response.StatusCode;
        }
    }
}

