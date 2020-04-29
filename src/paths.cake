public class BuildPathSettings
{
    public string ArtifactsDir { get; set; }
    public string TempArtifactsDir { get; set; }
    public string SrcDir { get; set; }
    public string TestDir { get; set; }
    public string BuildToolsDir { get; set; }
    public string BuildScriptsDir { get; set; }
    public string NuspecDir { get; set; }
    public string PackagesDir { get; set; }
    public string CommonAssemblyInfoDirectoryPath { get; set; }
    public string CommonAssemblyInfoFileName { get; set; }
    public string SolutionDirectoryPath { get; set; }
    public string SolutionFileName { get; set; }
}

public class BuildPaths
{
    public BuildPaths(
        ICakeContext context,
        BuildSettings settings,
        BuildPathSettings pathSettings,
        string projectName)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }
        if (pathSettings == null)
        {
            throw new ArgumentNullException("pathSettings");
        }
        if (string.IsNullOrEmpty(projectName))
        {
            throw new ArgumentException("projectName cannot be null or empty.");
        }

        Files       = new BuildFiles      (context, settings, pathSettings, projectName);
        Directories = new BuildDirectories(context, settings, pathSettings);
        Tools       = new ToolFiles       (context, settings, pathSettings, Directories);
    }

    public BuildFiles Files { get; private set; }

    public BuildDirectories Directories { get; private set; }

    public ToolFiles Tools { get; private set; }

    public void PrintToLog()
    {
        Directories.PrintToLog();
        Files.PrintToLog();
        Tools.PrintToLog();
    }
}

// Note: cake will resolve tools by its own
public class ToolFiles
{
    private readonly ICakeContext _context;

    public FilePath NuGet { get; private set; }

    public ToolFiles(
        ICakeContext  context,
        BuildSettings settings,
        BuildPathSettings pathSettings,
        BuildDirectories dirs)
    {
        _context = context;

        // Nuget is not cross-platform, because it does not run on .NET Core
        // We have to use .NET Framework on windows, and mono on mac/linux.
        NuGet = dirs.BuildTools.CombineWithFilePath("nuget.exe");
    }

    public void PrintToLog()
    {
        _context.Information("Tools configured:");
        _context.Information("  NuGet:  {0}", NuGet);
    }
}

public class BuildFiles
{
    private readonly ICakeContext _context;

    public FilePath Solution { get; private set; }
    public FilePath CommonAssemblyInfo { get; private set; }

    public BuildFiles(
        ICakeContext context,
        BuildSettings settings,
        BuildPathSettings pathSettings,
        string projectName)
    {
        _context = context;

        string solutionDirectoryPath = pathSettings.SolutionDirectoryPath ?? ".";
        string solutionFileName = pathSettings.SolutionFileName ?? projectName;
        if (string.IsNullOrEmpty(solutionFileName))
        {
            // solution file name cannot be empty because this triggers argumment exception in Cake.Core
            solutionFileName = "_Unspecfied_";
        }
        if (false == solutionFileName.EndsWith(".sln"))
        {
            solutionFileName = string.Concat(solutionFileName, ".sln");
        }

        Solution = ((DirectoryPath) solutionDirectoryPath).CombineWithFilePath(solutionFileName);

        string commonAssemblyInfoDirectoryPath = pathSettings.CommonAssemblyInfoDirectoryPath ?? "./src";
        string commonAssemblyInfoFileName = pathSettings.CommonAssemblyInfoFileName ?? "CommonAssemblyInfo.cs";

        CommonAssemblyInfo = ((DirectoryPath) commonAssemblyInfoDirectoryPath).CombineWithFilePath(commonAssemblyInfoFileName);
    }

    public void PrintToLog()
    {
        _context.Information("Files configured:");
        _context.Information("  Solution:           {0}", Solution);
        _context.Information("  CommonAssemblyInfo: {0}", CommonAssemblyInfo);
    }
}

public class BuildDirectories
{
    private readonly ICakeContext _context;

    public DirectoryPath Root { get; private set; }
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath TempArtifacts { get; private set; }
    public DirectoryPath Src { get; private set; }
    public DirectoryPath Test { get; private set; }
    public DirectoryPath BuildTools { get; private set; }
    public DirectoryPath BuildScripts { get; private set; }
    public DirectoryPath Nuspec { get; private set; }
    public DirectoryPath Packages { get; private set; }

    public BuildDirectories(
        ICakeContext context,
        BuildSettings settings,
        BuildPathSettings pathSettings
        )
    {
        _context = context;

        Root = context.MakeAbsolute(context.Environment.WorkingDirectory);
        Artifacts = pathSettings.ArtifactsDir ?? "./artifacts";
        TempArtifacts = pathSettings.TempArtifactsDir ?? Artifacts.Combine("temp");
        Src = pathSettings.SrcDir ?? "./src";
        Test = pathSettings.TestDir ?? "./test";
        BuildTools = pathSettings.BuildToolsDir ?? "./tools";
        BuildScripts = pathSettings.BuildScriptsDir ?? "./build";
        Nuspec = pathSettings.NuspecDir ?? "./nuspec";
        Packages = pathSettings.PackagesDir ?? "./packages";
    }

    public void PrintToLog()
    {
        _context.Information("Directories configured:");
        _context.Information("  Root:          {0}", Root);
        _context.Information("  Artifacts:     {0}", Artifacts);
        _context.Information("  TempArtifacts: {0}", TempArtifacts);
        // Relative paths are shown as 'artifacts', not './artifacts', also by GetRelativePath API
        //_context.Information("  Artifacts: {0}", Root.GetRelativePath(_context.MakeAbsolute(Artifacts)));
        _context.Information("  Src:           {0}", Src);
        _context.Information("  Test:          {0}", Test);
        _context.Information("  BuildTools:    {0}", BuildTools);
        _context.Information("  BuildScripts:  {0}", BuildScripts);
        _context.Information("  Nuspec:        {0}", Nuspec);
        _context.Information("  Packages:      {0}", Packages);
    }
}
