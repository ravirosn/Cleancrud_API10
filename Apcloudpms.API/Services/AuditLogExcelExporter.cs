using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using Apcloudpms.Application.DTOs;

namespace Apcloudpms.API.Services;

internal static class AuditLogExcelExporter
{
    private static readonly string[] Headers =
    [
        "Changed at (UTC)", "Entity", "Record", "Action", "Changed columns",
        "Old values", "New values", "Changed by", "IP address", "Trace ID"
    ];

    public static byte[] Create(IReadOnlyList<AuditLogItemDto> records)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteTextEntry(archive, "[Content_Types].xml", ContentTypes);
            WriteTextEntry(archive, "_rels/.rels", RootRelationships);
            WriteTextEntry(archive, "xl/workbook.xml", Workbook);
            WriteTextEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships);
            WriteTextEntry(archive, "xl/styles.xml", Styles);
            WriteWorksheet(archive, records);
        }

        return output.ToArray();
    }

    private static void WriteWorksheet(ZipArchive archive, IReadOnlyList<AuditLogItemDto> records)
    {
        var entry = archive.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            CloseOutput = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        writer.WriteStartElement("sheetViews");
        writer.WriteStartElement("sheetView");
        writer.WriteAttributeString("workbookViewId", "0");
        writer.WriteStartElement("pane");
        writer.WriteAttributeString("ySplit", "1");
        writer.WriteAttributeString("topLeftCell", "A2");
        writer.WriteAttributeString("activePane", "bottomLeft");
        writer.WriteAttributeString("state", "frozen");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();

        writer.WriteStartElement("cols");
        WriteColumn(writer, 1, 22);
        WriteColumn(writer, 2, 22);
        WriteColumn(writer, 3, 30);
        WriteColumn(writer, 4, 14);
        WriteColumn(writer, 5, 34);
        WriteColumn(writer, 6, 55);
        WriteColumn(writer, 7, 55);
        WriteColumn(writer, 8, 24);
        WriteColumn(writer, 9, 18);
        WriteColumn(writer, 10, 38);
        writer.WriteEndElement();

        writer.WriteStartElement("sheetData");
        writer.WriteStartElement("row");
        writer.WriteAttributeString("r", "1");
        for (var index = 0; index < Headers.Length; index++)
            WriteCell(writer, CellReference(index + 1, 1), Headers[index], 1);
        writer.WriteEndElement();

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            var rowNumber = index + 2;
            var values = new[]
            {
                record.ChangedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                record.EntityName,
                record.EntityDisplayName,
                record.Action,
                FormatJson(record.ChangedColumns),
                FormatJson(record.OldValues),
                FormatJson(record.NewValues),
                record.ChangedByName ?? "System",
                record.IpAddress ?? string.Empty,
                record.TraceId ?? string.Empty
            };

            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
            for (var column = 0; column < values.Length; column++)
                WriteCell(writer, CellReference(column + 1, rowNumber), values[column], 2);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteStartElement("autoFilter");
        writer.WriteAttributeString("ref", $"A1:J{Math.Max(1, records.Count + 1)}");
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static string FormatJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        try
        {
            using var document = JsonDocument.Parse(value);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static void WriteColumn(XmlWriter writer, int index, double width)
    {
        writer.WriteStartElement("col");
        writer.WriteAttributeString("min", index.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("max", index.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("width", width.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("customWidth", "1");
        writer.WriteEndElement();
    }

    private static void WriteCell(XmlWriter writer, string reference, string? value, int style)
    {
        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", reference);
        writer.WriteAttributeString("t", "inlineStr");
        writer.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
        writer.WriteStartElement("is");
        writer.WriteStartElement("t");
        writer.WriteAttributeString("xml", "space", null, "preserve");
        writer.WriteString(Sanitize(value));
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static string Sanitize(string? value) => string.IsNullOrEmpty(value)
        ? string.Empty
        : new string(value.Where(XmlConvert.IsXmlChar).Take(32_000).ToArray());

    private static string CellReference(int column, int row)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return name + row.ToString(CultureInfo.InvariantCulture);
    }

    private static void WriteTextEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private const string ContentTypes = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private const string RootRelationships = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string Workbook = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets><sheet name="Audit Logs" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """;

    private const string WorkbookRelationships = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private const string Styles = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="2">
            <font><sz val="10"/><name val="Aptos"/></font>
            <font><b/><color rgb="FFFFFFFF"/><sz val="10"/><name val="Aptos"/></font>
          </fonts>
          <fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF206BC4"/><bgColor indexed="64"/></patternFill></fill></fills>
          <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="3">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="top" wrapText="1"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;
}
