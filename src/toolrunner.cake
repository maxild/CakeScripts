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

    public string Command(string args)
    {
        return CommandHelper(args);
    }

    public string Command(string formatArgs, string arg1)
    {
        return CommandHelper(string.Format(formatArgs, arg1));
    }

    public string Command(string formatArgs, string arg1, string arg2)
    {
        return CommandHelper(string.Format(formatArgs, arg1, arg2));
    }

    public string Command(string formatArgs, string arg1, string arg2, string arg3)
    {
        return CommandHelper(string.Format(formatArgs, arg1, arg2, arg3));
    }

    public string Command(string formatArgs, params string[] args)
    {
        return CommandHelper(string.Format(formatArgs, args));
    }

    public string SafeCommand(string args)
    {
        return CommandHelper(args, neverThrowOnErrorStatusCode: true);
    }

    public string SafeCommand(string formatArgs, string arg1)
    {
        return CommandHelper(string.Format(formatArgs, arg1), neverThrowOnErrorStatusCode: true);
    }

    public string SafeCommand(string formatArgs, string arg1, string arg2)
    {
        return CommandHelper(string.Format(formatArgs, arg1, arg2), neverThrowOnErrorStatusCode: true);
    }

    public string SafeCommand(string formatArgs, string arg1, string arg2, string arg3)
    {
        return CommandHelper(string.Format(formatArgs, arg1, arg2, arg3), neverThrowOnErrorStatusCode: true);
    }

    public string SafeCommand(string formatArgs, params string[] args)
    {
        return CommandHelper(string.Format(formatArgs, args), neverThrowOnErrorStatusCode: true);
    }

    /// <summary>
    ///  Run the tool with the given arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The output written to stdout.</returns>
    string CommandHelper(string args, bool neverThrowOnErrorStatusCode = false)
    {
        IEnumerable<string> stdout, stderr;
        int exit = _context.StartProcess(ToolPath,
            new ProcessSettings {
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }, out stdout, out stderr);

        string stdoutResult = (string.Join(NewLineToken.ToString(), stdout) ?? string.Empty).Trim();

        if (exit != 0)
        {
            string stderrResult = (string.Join(NewLineToken.ToString(), stderr) ?? string.Empty).Trim();

            if (neverThrowOnErrorStatusCode) {
                return stderrResult;
            }

            string message = string.IsNullOrEmpty(stderrResult) ? stdoutResult : stderrResult;

            throw new InvalidOperationException(string.Format("'{0} {1}' exited with status {2}, and message: {3}",
                ToolPath, args, exit, message));
        }

        return stdoutResult;
    }
}
