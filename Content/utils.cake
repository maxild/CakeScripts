public static class StringUtils
{
    public static string TrimEnd(string s, string suffixToRemove)
    {
        if (s.EndsWith(suffixToRemove))
        {
            return s.Substring(0, s.Length - suffixToRemove.Length);
        }
        return s;
    }
}
