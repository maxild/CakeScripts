#tool nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0007

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

    public string NuGetVersion {
        get
        {
            if (string.IsNullOrEmpty(VersionSuffix))
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

    public string PatchedVersion { get { return string.Concat(MajorMinorPatch, "-*"); } }

        public void PrintToLog()
    {
        _context.Information("Version Information:");
        _context.Information("  MajorMinorPatch:              {0}", MajorMinorPatch);
        _context.Information("  VersionSuffix:                {0}", VersionSuffix);
        _context.Information("  NuGetVersion:                 {0}", NuGetVersion);
        _context.Information("  AssemblyVersion:              {0}", AssemblyVersion);
        _context.Information("  AssemblyFileVersion:          {0}", AssemblyFileVersion);
        _context.Information("  AssemblyInformationalVersion: {0}", AssemblyInformationalVersion);
        _context.Information("  SemVer:                       {0}", SemVer);
    }

    public static GitVersionInfo Calculate(ICakeContext context, BuildSystem buildSystem)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string majorMinorPatch, pkgVersion, semVer, infoVer;

        // TODO: GitVersion running on Unix (.NET Core)
        if (context.IsRunningOnWindows())
        {
            context.Information("Calculating Semantic Version...");

            if (false == buildSystem.IsLocalBuild)
            {
                context.GitVersion(new GitVersionSettings
                {
                    OutputType = GitVersionOutput.BuildServer
                });

                majorMinorPatch = context.EnvironmentVariable("GitVersion_MajorMinorPatch");
                pkgVersion = context.EnvironmentVariable("GitVersion_LegacySemVerPadded");
                semVer = context.EnvironmentVariable("GitVersion_SemVer");
                infoVer = context.EnvironmentVariable("GitVersion_InformationalVersion");
            }
            else
            {
                var assertedVersions = context.GitVersion(new GitVersionSettings
                {
                    OutputType = GitVersionOutput.Json
                });

                majorMinorPatch = assertedVersions.MajorMinorPatch;
                pkgVersion = assertedVersions.LegacySemVerPadded;
                semVer = assertedVersions.SemVer;
                infoVer = assertedVersions.InformationalVersion;
            }
        }
        else
        {
            majorMinorPatch = "0.0.0";
            pkgVersion = string.Concat(majorMinorPatch, "-not-windows");
            semVer = string.Concat(majorMinorPatch, "-not.windows");
            infoVer = string.Concat(semVer, "+GitVersion.Was.Not.Called");
        }

        return new GitVersionInfo(context)
        {
            MajorMinorPatch = majorMinorPatch,
            VersionSuffix = pkgVersion.Substring(majorMinorPatch.Length).TrimStart('-'),
            AssemblyVersion = majorMinorPatch,
            AssemblyFileVersion = string.Concat(majorMinorPatch, ".0"),
            AssemblyInformationalVersion = infoVer,
            SemVer = semVer
        };
    }
}

