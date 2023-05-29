namespace Cubist.Germanium.CSharp;

internal static class StringExtensions
{
    public static string ToPascalCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        if (str.Length == 1) return str.ToUpperInvariant();
        return str.Substring(0, 1).ToUpperInvariant() + str.Substring(1);
    }

    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        if (str.Length == 1) return str.ToLowerInvariant();
        return str.Substring(0, 1).ToLowerInvariant() + str.Substring(1);
    }

    
    public static string JoinWith<T>(this IEnumerable<T> items, string separator)
    {
        return string.Join(separator, items);
    }

    public static string TrimSuffix(this string s, string suffix, StringComparison comparison = StringComparison.CurrentCulture)
    {
        if (s == null) return null;
        if (string.IsNullOrEmpty(suffix)) return s;
        if (s.EndsWith(suffix, comparison))
            return s.Substring(0, s.Length - suffix.Length);
        return s;
    }
    public static ReadOnlySpan<char> TrimSuffix(this ReadOnlySpan<char> s, ReadOnlySpan<char> suffix, StringComparison comparison = StringComparison.CurrentCulture)
    {
        if (s == null) return null;
        if (suffix.IsEmpty) return s;
        if (s.EndsWith(suffix, comparison))
            return s.Slice(0, s.Length - suffix.Length);
        return s;
    }
    public static string TrimPrefix(this string s, string prefix, StringComparison comparison = StringComparison.CurrentCulture)
    {
        if (s == null) return null;
        if (string.IsNullOrEmpty(prefix)) return s;
        if (s.StartsWith(prefix, comparison))
            return s.Substring(prefix.Length);
        return s;
    }
    public static ReadOnlySpan<char> TrimPrefix(this ReadOnlySpan<char> s, ReadOnlySpan<char> prefix, StringComparison comparison = StringComparison.CurrentCulture)
    {
        if (s == null) return null;
        if (prefix.IsEmpty) return s;
        if (s.StartsWith(prefix, comparison))
            return s.Slice(prefix.Length);
        return s;
    }
}