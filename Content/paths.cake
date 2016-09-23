public class BuildPathSettings
{
    public string ArtifactsDir { get; set; }
    public string SrcDir { get; set; }
    public string TestDir { get; set; }
    public string BuildToolsDir { get; set; }
    public string BuildScriptsDir { get; set; }
    public string DotNetDir { get; set; }
    public string NuspecDir { get; set; }
    public string PackagesDir { get; set; }
    public string CommonAssemblyInfoDirectoryPath { get; set; }
    public string CommonAssemblyInfoFileName { get; set; }
}

public class BuildPaths
{
    public BuildPaths(ICakeContext context, BuildPathSettings settings)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        Files = new BuildFiles(context, settings ?? new BuildPathSettings());
        Directories = new BuildDirectories(context, settings ?? new BuildPathSettings());
        Tools = new ToolFiles(context, settings ?? new BuildPathSettings(), Directories);
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

    public FilePath DotNet { get; private set; }
    public FilePath NuGet { get; private set; }
    // TODO: Git

    public ToolFiles(ICakeContext  context, BuildPathSettings settings, BuildDirectories dirs)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        _context = context;

        DotNet = context.IsRunningOnWindows()
                ? dirs.DotNet.CombineWithFilePath("dotnet.exe")
                : dirs.DotNet.CombineWithFilePath("dotnet");
        NuGet = dirs.BuildTools.CombineWithFilePath("nuget.exe"); // TODO: Cross-plat???
    }

    public void PrintToLog()
    {
        _context.Information("Tools configured:");
        _context.Information("  DotNet: {0}", DotNet);
        _context.Information("  NuGet:  {0}", NuGet);
    }
}

public class BuildFiles
{
    private readonly ICakeContext _context;

    public FilePath CommonAssemblyInfo { get; private set; }

    public BuildFiles(ICakeContext context, BuildPathSettings settings)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        _context = context;

        string commonAssemblyInfoDirectoryPath = settings.CommonAssemblyInfoDirectoryPath ?? "./src";
        string commonAssemblyInfoFileName = settings.CommonAssemblyInfoFileName ?? "CommonAssemblyInfo.cs";
        CommonAssemblyInfo = ((DirectoryPath) commonAssemblyInfoDirectoryPath).CombineWithFilePath(commonAssemblyInfoFileName);;
    }

    public void PrintToLog()
    {
        _context.Information("Files configured:");
        _context.Information("  CommonAssemblyInfo: {0}", CommonAssemblyInfo);
    }
}

public class BuildDirectories
{
    private readonly ICakeContext _context;

    public DirectoryPath Root { get; private set; }
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath Src { get; private set; }
    public DirectoryPath Test { get; private set; }
    public DirectoryPath BuildTools { get; private set; }
    public DirectoryPath BuildScripts { get; private set; }
    public DirectoryPath DotNet { get; private set; }
    public DirectoryPath Nuspec { get; private set; }
    public DirectoryPath Packages { get; private set; }

    public BuildDirectories(ICakeContext context, BuildPathSettings settings)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }

        _context = context;

        Root = context.MakeAbsolute(context.Environment.WorkingDirectory);
        Artifacts = settings.ArtifactsDir ?? "./artifacts";
        Src = settings.SrcDir ?? "./src";
        Test = settings.TestDir ?? "./test";
        BuildTools = settings.BuildToolsDir ?? "./.tools";
        BuildScripts = settings.BuildScriptsDir ?? "./build";
        DotNet = settings.DotNetDir ?? "./.dotnet";
        Nuspec = settings.NuspecDir ?? "./nuspec";
        Packages = settings.PackagesDir ?? "./packages";
    }

    public void PrintToLog()
    {
        _context.Information("Directories configured:");
        _context.Information("  Root:         {0}", Root);
        _context.Information("  Artifacts:    {0}", Artifacts);
        // Relative paths are shown as 'artifacts', not './artifacts', also by GetRelativePath API
        //_context.Information("  Artifacts: {0}", Root.GetRelativePath(_context.MakeAbsolute(Artifacts)));
        _context.Information("  Src:          {0}", Src);
        _context.Information("  Test:         {0}", Test);
        _context.Information("  BuildTools:   {0}", BuildTools);
        _context.Information("  BuildScripts: {0}", BuildScripts);
        _context.Information("  DotNet:       {0}", DotNet);
        _context.Information("  Nuspec:       {0}", Nuspec);
        _context.Information("  Packages:     {0}", Packages);
    }
}
