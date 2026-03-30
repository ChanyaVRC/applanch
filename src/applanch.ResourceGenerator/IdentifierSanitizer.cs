using System.Collections.Generic;
using System.Text;

namespace applanch.ResourceGenerator;

internal static class IdentifierSanitizer
{
    private static readonly HashSet<string> CSharpKeywords = new(System.StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
        "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
        "ushort", "using", "virtual", "void", "volatile", "while"
    };

    public static string Sanitize(string name)
    {
        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            if (i == 0)
            {
                if (IsIdentifierStart(ch))
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.Append('_');
                    if (IsIdentifierPart(ch))
                    {
                        builder.Append(ch);
                    }
                }

                continue;
            }

            builder.Append(IsIdentifierPart(ch) ? ch : '_');
        }

        if (builder.Length == 0)
        {
            builder.Append("Resource");
        }

        var identifier = builder.ToString();
        return CSharpKeywords.Contains(identifier) ? "@" + identifier : identifier;
    }

    private static bool IsIdentifierStart(char ch) =>
        ch == '_' || char.IsLetter(ch);

    private static bool IsIdentifierPart(char ch) =>
        ch == '_' || char.IsLetterOrDigit(ch);
}
