namespace MoVALiveViewer.Parsing;

public sealed class LineSplitter
{
    private string _carry = string.Empty;

    public IEnumerable<string> Feed(string chunk)
    {
        var text = _carry + chunk;
        _carry = string.Empty;

        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                int end = i;
                if (end > start && text[end - 1] == '\r')
                    end--;
                yield return text[start..end];
                start = i + 1;
            }
        }

        if (start < text.Length)
            _carry = text[start..];
    }

    public string? Flush()
    {
        if (_carry.Length == 0) return null;
        var line = _carry;
        _carry = string.Empty;
        return line;
    }

    public void Reset()
    {
        _carry = string.Empty;
    }
}
