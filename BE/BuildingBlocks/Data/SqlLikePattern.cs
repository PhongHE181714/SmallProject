namespace BE.Data;

public static class SqlLikePattern
{
    public const char ESCAPE_CHARACTER = '~';

    public static string Escape(string value)
    {
        return value
            .Replace(ESCAPE_CHARACTER.ToString(), new string(ESCAPE_CHARACTER, 2))
            .Replace("%", $"{ESCAPE_CHARACTER}%")
            .Replace("_", $"{ESCAPE_CHARACTER}_")
            .Replace("[", $"{ESCAPE_CHARACTER}[");
    }
}
