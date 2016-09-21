#tool nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0007

public class BuildVersion
{
    public class PackageVersion
    {
        public PackageVersion(string version, string versionSuffix)
        {
            Version = version;
            VersionSuffix = versionSuffix;
        }

        // version (without any pre-release tag)
        public string Version { get; private set; }

        // --version-suffix on dotnet-pack
        public string VersionSuffix { get; private set; }

        // Version to patch into project.json
        public string PatchedVersion { get { return string.Concat(Version, "-*"); } }
    }

    public class AssemblyVersion
    {
        public AssemblyVersion(string version, string fileVersion, string informationalVersion)
        {
            Version = version;
            FileVersion = fileVersion;
            InformationalVersion = informationalVersion;
        }
        public string Version { get; private set; }
        public string FileVersion { get; private set; }
        public string InformationalVersion { get; private set; }
    }

    // To be used with patching project.json with and dotnet pack --version-suffix
    public PackageVersion Package { get; private set; }

    // The [Assembly*Version] attributes are generated by dotnet-build by default
    // (i.e. dotnet-compile.assemblyinfo.cs is generated by dotnet SDK tooling).
    //    Can this be changed? I think the tooling will search for any of the attributes
    //    and only generate any not found.
    public AssemblyVersion Assembly { get; private set; }

    public string SemVersion { get; private set; }
    public string InformationalVersion { get; private set; }

    public string CakeVersion { get; private set; }

    public static BuildVersion Calculate(ICakeContext context, BuildParameters parameters)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string version = null;
        string pkgVersion = null;
        string semVersion = null;
        string infoVersion = null;
        string assemblyVersion = null;

        if (context.IsRunningOnWindows())
        {
            context.Information("Calculating version.");

            if (parameters.IsLocalBuild)
            {
                GitVersion assertedVersions = context.GitVersion(new GitVersionSettings
                {
                    OutputType = GitVersionOutput.Json,
                });

                version = assertedVersions.MajorMinorPatch;
                pkgVersion = assertedVersions.LegacySemVerPadded;
                semVersion = assertedVersions.SemVer;
                infoVersion = assertedVersions.InformationalVersion;
                assemblyVersion = assertedVersions.AssemblySemVer;
            }
            else
            {
                context.GitVersion(new GitVersionSettings{
                    OutputType = GitVersionOutput.BuildServer
                });

                version = context.EnvironmentVariable("GitVersion_MajorMinorPatch");
                pkgVersion = context.EnvironmentVariable("GitVersion_LegacySemVerPadded");
                semVersion = context.EnvironmentVariable("GitVersion_SemVer");
                infoVersion = context.EnvironmentVariable("GitVersion_InformationalVersion");
                assemblyVersion = context.EnvironmentVariable("GitVersion_AssemblySemVer");
            }
        }

        // If skipGitVersion setting is implemented this block of code is important
        if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(semVersion))
        {
            context.Information("Fetching version from first project.json...");

            version = ReadProjectJsonVersion(context);
            pkgVersion = version;
            semVersion = version;
            infoVersion = version;
            assemblyVersion = version;
        }

        return new BuildVersion
        {
            Package = new PackageVersion(version, pkgVersion.Substring(version.Length).TrimStart('-')),
            Assembly = new AssemblyVersion(assemblyVersion, string.Concat(version, ".0"), infoVersion),
            SemVersion = semVersion,
            InformationalVersion = infoVersion,
            CakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString()
        };
    }

    public static string ReadProjectJsonVersion(ICakeContext context)
    {
        var projects = context.GetFiles("./src/**/project.json");
        foreach (var project in projects)
        {
            return ReadProjectJsonVersion(project.FullPath);
        }
        throw new CakeException("Could not find any project.json files.");
    }

    public static string ReadProjectJsonVersion(string projectJsonPath)
    {
        var content = System.IO.File.ReadAllText(projectJsonPath, Encoding.UTF8);
        var node = Newtonsoft.Json.Linq.JObject.Parse(content);
        if (node["version"] != null)
        {
            var version = node["version"].ToString();
            return version.Replace("-*", "");
        }
        throw new CakeException("Could not parse version.");
    }

    public static bool PatchProjectJson(FilePath project, string version)
    {
        // var content = System.IO.File.ReadAllText(project.FullPath, Encoding.UTF8);
        // var node = Newtonsoft.Json.Linq.JObject.Parse(content);
        // if (node["version"] != null)
        // {
        //     node["version"].Replace(version);
        //     System.IO.File.WriteAllText(project.FullPath, node.ToString(), Encoding.UTF8);
        //     return true;
        // };
        // return false;

        bool versionFound = false;
        Newtonsoft.Json.Linq.JObject node;

        using (var file = new System.IO.FileStream(project.FullPath, FileMode.Open))
        using (var stream = new System.IO.StreamReader(file))
        using (var json = new Newtonsoft.Json.JsonTextReader(stream))
        {
            node = Newtonsoft.Json.Linq.JObject.Load(json);
        }

        var versionAttr = node.Property("version");
        if (versionAttr == null)
        {
            node.Add("version", new Newtonsoft.Json.Linq.JValue(version));
            versionFound = false;
        }
        else
        {
            versionAttr.Value = version;
            versionFound = true;
        }

        System.IO.File.WriteAllText(project.FullPath, node.ToString(), Encoding.UTF8);

        return versionFound;
    }
}

///////


public class BuildVersion
{
    public string Version { get; private set; }
    public string SemVersion { get; private set; }
    public string Milestone { get; private set; }
    public string CakeVersion { get; private set; }

    public static BuildVersion CalculatingSemanticVersion(
        ICakeContext context,
        BuildParameters parameters
        )
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string version = null;
        string semVersion = null;
        string milestone = null;

        if (context.IsRunningOnWindows())
        {
            context.Information("Calculating Semantic Version...");
            if (!parameters.IsLocalBuild || parameters.IsPublishBuild || parameters.IsReleaseBuild)
            {
                context.GitVersion(new GitVersionSettings{
                    UpdateAssemblyInfoFilePath = parameters.Paths.Files.SolutionInfoFilePath,
                    UpdateAssemblyInfo = true,
                    OutputType = GitVersionOutput.BuildServer
                });

                version = context.EnvironmentVariable("GitVersion_MajorMinorPatch");
                semVersion = context.EnvironmentVariable("GitVersion_LegacySemVerPadded");
                milestone = string.Concat(version);
            }

            GitVersion assertedVersions = context.GitVersion(new GitVersionSettings
            {
                OutputType = GitVersionOutput.Json,
            });

            version = assertedVersions.MajorMinorPatch;
            semVersion = assertedVersions.LegacySemVerPadded;
            milestone = string.Concat(version);

            context.Information("Calculated Semantic Version: {0}", semVersion);
        }

        if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(semVersion))
        {
            context.Information("Fetching version from SolutionInfo...");
            var assemblyInfo = context.ParseAssemblyInfo(parameters.Paths.Files.SolutionInfoFilePath);
            version = assemblyInfo.AssemblyVersion;
            semVersion = assemblyInfo.AssemblyInformationalVersion;
            milestone = string.Concat(version);
        }

        var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();

        return new BuildVersion
        {
            Version = version,
            SemVersion = semVersion,
            Milestone = milestone,
            CakeVersion = cakeVersion
        };
    }
}
