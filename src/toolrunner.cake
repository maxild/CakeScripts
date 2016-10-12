public class ToolRunner
{
    //const string DEFAULT_NEWLINE = "\n";
    //const string DEFAULT_NEWLINE = "\r\n";

    private readonly ICakeContext _context;

    public ToolRunner(ICakeContext context, IEnumerable<string> toolNames, string newLineToken = null)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
        if (toolNames == null)
        {
            throw new ArgumentNullException("toolNames");
        }

        FilePath toolPath = null;
        foreach (var toolName in toolNames)
        {
            toolPath = context.Tools.Resolve(toolName);
            if (toolPath != null)
            {
                break;
            }
        }

        if (toolPath == null)
        {
            throw new InvalidOperationException(
                string.Format("Cake could not resolve the PATH to any of the given tool names: {0}.",
                    string.Join(", ", toolNames)));
        }

        _context = context;
        ToolPath = toolPath;
        NewLineToken = newLineToken ?? Environment.NewLine;
    }

    public FilePath ToolPath { get; private set; }

    public string NewLineToken { get; private set; }

    /// <summary>
    ///  Run the tool with the given arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The exit status.</returns>
    int Run(string args)
    {
        return Run(args, new ProcessSettings());
    }

    /// <summary>
    ///  Run the tool with the given arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>The exit status.</returns>
    int Run(string args, string workingDirectory)
    {
        return Run(args, new ProcessSettings { WorkingDirectory = workingDirectory});
    }

    /// <summary>
    ///  Run the tool with the given arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The exit status.</returns>
    int Run(string args, ProcessSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException("settings");
        }
        settings.Arguments = args;
        return _context.StartProcess(ToolPath, settings);
    }

    /// <summary>
    ///  Run the tool with the given arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The output written to stdout.</returns>
    public string Command(string args)
    {
        IEnumerable<string> stdout;
        int exit = _context.StartProcess(ToolPath,
            new ProcessSettings {
                Arguments = args,
                RedirectStandardOutput = true,
            }, out stdout);

        if (exit != 0)
        {
            throw new InvalidOperationException(string.Format("'{0} {1}' exited with status {2}, and message: {3}",
                ToolPath, args, exit, string.Join(Environment.NewLine, stdout)));

        }

        string result = (string.Join(NewLineToken.ToString(), stdout) ?? string.Empty).Trim();
        return result;
    }
}
