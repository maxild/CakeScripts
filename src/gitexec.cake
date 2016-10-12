public class GitExec
{
    const char SEP = '#';
    private readonly ICakeContext _context;
    private readonly string _gitPath;

    public GitExec(ICakeContext context, char? newLineToken = null)
    {
        _context = context;
        var path = context.Tools.Resolve("git.exe").FullPath;
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Cake could not resolve the PATH to git.exe");
        }
        Path = path;
        NewLineToken = newLineToken ?? SEP;
    }

    public string Path { get; private set; }

    public char NewLineToken { get; private set; }

    // TODO: Move to runhelpers.cake (where stdout capturing command is missing)
    public string Command(string arguments)
    {
        //_context.Information("git {0}", arguments);
        IEnumerable<string> stdout;
        int exit = _context.StartProcess(Path, new ProcessSettings {
            Arguments = arguments,
            RedirectStandardOutput = true,
        }, out stdout);
        string result = exit == 0 ? (string.Join(NewLineToken.ToString(), stdout) ?? string.Empty).Trim() : string.Empty;
        //_context.Information("git {0} --> {1}", arguments, result);
        return result;
    }
}
