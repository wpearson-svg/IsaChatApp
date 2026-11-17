using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace IsaChatApp.Services;

/// <summary>
/// Writes a plain Excel workbook (.xlsx) with no outside library. An .xlsx file is a zip of a few XML files; this
/// writes the smallest set Excel, LibreOffice and Google Sheets open without complaint: one sheet per table, a bold
/// first row, numbers stored as numbers, text as text, and column widths fitted to the longest value.
/// Added 16 November 2026 for the Content Health Dashboard's "Export to xlsx" button (ContentHealthExport.cs).
/// </summary>
public sealed class XlsxWriter
{
    /// <summary>One worksheet. Rows hold string, int, long, double or null; the header row is bold.</summary>
    public sealed record Sheet(string Name, IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<object?>> Rows);

    private readonly List<Sheet> _sheets = new();

    public XlsxWriter Add(string name, IReadOnlyList<string> header, IEnumerable<IReadOnlyList<object?>> rows)
    {
        _sheets.Add(new Sheet(UniqueName(name), header, rows.ToList()));
        return this;
    }

    public byte[] ToBytes()
    {
        if (_sheets.Count == 0) throw new InvalidOperationException("A workbook needs at least one sheet.");
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var sheetOverrides = string.Concat(_sheets.Select((_, i) =>
                $"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"));
            Write(zip, "[Content_Types].xml",
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                sheetOverrides + "</Types>");
            Write(zip, "_rels/.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            Write(zip, "xl/workbook.xml",
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>" +
                string.Concat(_sheets.Select((s, i) => $"<sheet name=\"{Esc(s.Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>")) +
                "</sheets></workbook>");
            Write(zip, "xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                string.Concat(_sheets.Select((_, i) =>
                    $"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>")) +
                $"<Relationship Id=\"rId{_sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "</Relationships>");
            // Style 0: normal. Style 1: bold (the header row).
            Write(zip, "xl/styles.xml",
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs>" +
                "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                "</styleSheet>");
            for (var i = 0; i < _sheets.Count; i++) Write(zip, $"xl/worksheets/sheet{i + 1}.xml", SheetXml(_sheets[i]));
        }
        return ms.ToArray();
    }

    private static string SheetXml(Sheet s)
    {
        var all = new List<IReadOnlyList<object?>> { s.Header.Cast<object?>().ToList() };
        all.AddRange(s.Rows);
        var cols = all.Max(r => r.Count);
        var sb = new StringBuilder("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        if (cols > 0)
        {
            sb.Append("<cols>");
            for (var c = 0; c < cols; c++)
            {
                var longest = all.Max(r => c < r.Count ? Text(r[c]).Length : 0);
                var width = Math.Clamp(longest + 2, 8, 80);
                sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{width}\" customWidth=\"1\"/>");
            }
            sb.Append("</cols>");
        }
        sb.Append("<sheetData>");
        for (var r = 0; r < all.Count; r++)
        {
            sb.Append($"<row r=\"{r + 1}\">");
            for (var c = 0; c < all[r].Count; c++)
            {
                var v = all[r][c];
                if (v is null) continue;
                var cellRef = Column(c) + (r + 1);
                var style = r == 0 ? " s=\"1\"" : "";
                if (v is int or long or double or float or decimal)
                    sb.Append($"<c r=\"{cellRef}\"{style}><v>{Convert.ToDouble(v, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                else
                    sb.Append($"<c r=\"{cellRef}\"{style} t=\"inlineStr\"><is><t xml:space=\"preserve\">{Esc(Text(v))}</t></is></c>");
            }
            sb.Append("</row>");
        }
        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string Text(object? v) => v switch
    {
        null => "",
        double d => d.ToString("0.##", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => v.ToString() ?? ""
    };

    // A, B, ... Z, AA, AB ...
    private static string Column(int index)
    {
        var s = "";
        for (index++; index > 0; index = (index - 1) / 26) s = (char)('A' + (index - 1) % 26) + s;
        return s;
    }

    // XML-escape, and drop control characters XML 1.0 does not allow (a stray one would make Excel refuse the file).
    private static string Esc(string s) =>
        SecurityElement.Escape(new string(s.Where(ch => ch == '\t' || ch == '\n' || ch == '\r' || ch >= ' ').ToArray())) ?? "";

    // Excel sheet names: at most 31 characters, none of : \ / ? * [ ], and unique in the workbook.
    private string UniqueName(string name)
    {
        var clean = new string(name.Where(ch => ":\\/?*[]".IndexOf(ch) < 0).ToArray()).Trim();
        if (clean.Length == 0) clean = "Sheet";
        if (clean.Length > 31) clean = clean[..31];
        var candidate = clean;
        for (var n = 2; _sheets.Any(s => s.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)); n++)
            candidate = (clean.Length > 28 ? clean[..28] : clean) + " " + n;
        return candidate;
    }

    private static void Write(ZipArchive zip, string path, string xml)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        w.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        w.Write(xml);
    }
}
