// should be ext method....not possible until cake is running on CoreCLR with up to date Roslyn bits
public class FailureHelper
{
    public static void ExceptionOnError(int exitCode, string errorMsg)
    {
        if (exitCode != 0)
        {
            throw new System.Exception(errorMsg);
        }
    }
}
