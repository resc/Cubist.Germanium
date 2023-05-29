using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Cubist.Germanium.CSharp;

/// <summary> CodeWriter keeps track of indentation and blocks, to simplify writing code. </summary>
internal class CodeWriter : TextWriter
{ 
    private readonly TextWriter _w;
    private char _newline;
    private char[] _ignored = Array.Empty<char>();
    private bool _writeIndent;
    private int _indentLevel;

    public CodeWriter() : this(new StringWriter())
    {
    }

    public CodeWriter(Stream s) : this(new StreamWriter(s))
    {
    }

    public CodeWriter(TextWriter w)
    {
        _w = w;
        SetNewLine(w.NewLine);
        LinePrefix = "";
        IndentText = "    ";
        BlockStart = "{";
        BlockEnd = "}";
        BlockCommentStart = "/*";
        BlockCommentEnd = "*/";
        CommentStart = "// ";
        CommentEnd = w.NewLine;
    }

    public string LinePrefix { get; set; }
    public string IndentText { get; set; }

    public string BlockStart { get; set; }
    public string BlockEnd { get; set; }

    public string BlockCommentStart { get; set; }
    public string BlockCommentEnd { get; set; }

    public string CommentStart { get; set; }
    public string CommentEnd { get; set; }

    public override Encoding Encoding => _w.Encoding;

    public override string NewLine
    {
        get => _w.NewLine;
        set => SetNewLine(value);
    }

    public override void Write(char value)
    {
        WriteIndent();
        if (value == _newline)
            _writeIndent = true;

        // normalize line endings...
        if (value == _newline)
            _w.Write(NewLine);
        else if (!_ignored.Contains(value))
            _w.Write(value);
    }

    /// <summary> Indents the code a level </summary>
    public Scope Indent()
    {
        _indentLevel++;
        return Scope.Create(Dedent);
    }

    /// <summary> Writes a block </summary>
    /// <param name="trailer">text to immediatly follow the closing brace, before the newline. usually a ;</param>
    public Scope Block(string trailer = null)
    {
        WriteLine(BlockStart);
        var d = Indent();
        return Scope.Create(() =>
        {
            d.Dispose();
            WriteLine($"{BlockEnd}{trailer}");
        });
    }

    /// <summary> Writes a block comment</summary>
    public Scope BlockComment()
    {
        WriteLine(BlockCommentStart);
        var d = Indent();
        return Scope.Create(() =>
        {
            d.Dispose();
            WriteLine($"{BlockCommentEnd}");
        });
    }

    public void WriteComment(string comment)
    {
        Write(CommentStart);
        Write(comment.Replace("\n", $"{NewLine}").Replace(CommentEnd, $"{NewLine}{CommentStart}"));
        Write(CommentEnd);
    }

    public override void Flush() => _w.Flush();

    public override string ToString() => _w.ToString() ?? "";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush();
            _w.Dispose();
        }

        base.Dispose(disposing);
    }

    private void SetNewLine(string value)
    {
        _w.NewLine = value;
        _newline = _w.NewLine.ToCharArray().Last();
        _ignored = _w.NewLine.ToCharArray().Except(new[] { _newline }).ToArray();
    }

    private void Dedent() => _indentLevel--;


    public Scope UseLinePrefix(string prefix, bool prependCurrentIndent = true)
    {
        var oldPrefix = LinePrefix;
        var oldIndentLevel = _indentLevel;

        var currentIndent = Enumerable.Repeat(IndentText, _indentLevel).JoinWith("");
        if (prependCurrentIndent)
        {
            prefix = currentIndent + prefix;
        }
        else
        {
            if (prefix.Length < currentIndent.Length)
            {
                prefix += currentIndent.Substring(prefix.Length);
            }
        }

        LinePrefix = prefix;
        _indentLevel = 0;
        return Scope.Create(() =>
        {
            LinePrefix = oldPrefix;
            _indentLevel = oldIndentLevel;
        });
    }
        
    private void WriteIndent()
    {
        if (_writeIndent)
        {
            if (!string.IsNullOrEmpty(LinePrefix))
                _w.Write(LinePrefix);

            for (int i = 0; i < _indentLevel; i++)
                _w.Write(IndentText);

            _writeIndent = false;
        }
    }


}