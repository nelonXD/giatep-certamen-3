using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
namespace Giatep.Api.Services;

public interface IExportador { string Formato { get; } string Mime { get; } byte[] Exportar(Reporte reporte); }
public static class TextoReporte
{
    public static string Valor(object? value) => value is DateOnly fecha ? fecha.ToString("yyyy-MM-dd") : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
    public static string Seguro(string value) => value.Length > 0 && "=+-@\t\r\n".Contains(value[0]) ? "'" + value : value;
}
public sealed class CsvExportador : IExportador
{
    public string Formato => "csv"; public string Mime => "text/csv; charset=utf-8";
    public byte[] Exportar(Reporte r) {
        string Row(IEnumerable<object?> cells) => string.Join(';', cells.Select(v => "\"" + TextoReporte.Seguro(TextoReporte.Valor(v)).Replace("\"", "\"\"") + "\""));
        var texto = Row(r.Columnas) + "\r\n" + string.Join("\r\n", r.Filas.Select(Row));
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(texto)];
    }
}
public sealed class ExcelExportador : IExportador
{
    public string Formato => "xlsx"; public string Mime => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public byte[] Exportar(Reporte r)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) {
            void Entry(string name, string value) { using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); w.Write(value); }
            string Text(object? value) => SecurityElement.Escape(TextoReporte.Valor(value)) ?? "";
            string Cell(object? v, int style = 0) => v switch {
                DateOnly date => $"<c s=\"2\"><v>{date.ToDateTime(TimeOnly.MinValue).ToOADate().ToString(CultureInfo.InvariantCulture)}</v></c>",
                int or long or double or decimal => $"<c s=\"3\"><v>{TextoReporte.Valor(v)}</v></c>",
                _ => $"<c t=\"inlineStr\" s=\"{style}\"><is><t xml:space=\"preserve\">{Text(v)}</t></is></c>"
            };
            var rows = new StringBuilder();
            void Row(IEnumerable<object?> values, int style = 0) { rows.Append("<row>"); foreach (var value in values) rows.Append(Cell(value, style)); rows.Append("</row>"); }
            Row([r.Titulo], 1); Row([$"Emisión: {DateTimeOffset.Now:yyyy-MM-dd HH:mm}"]); Row([r.Filtros]); Row(r.Columnas, 1); foreach (var fila in r.Filas) Row(fila); Row([$"Total de casos: {r.TotalCasos}. Filas: {r.Filas.Count}"]);
            Entry("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
            Entry("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Entry("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Reporte\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Entry("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            Entry("xl/styles.xml", "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"4\"><xf xfId=\"0\"/><xf fontId=\"1\" xfId=\"0\" applyFont=\"1\"/><xf numFmtId=\"14\" xfId=\"0\" applyNumberFormat=\"1\"/><xf numFmtId=\"3\" xfId=\"0\" applyNumberFormat=\"1\"/></cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");
            Entry("xl/worksheets/sheet1.xml", $"<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"4\" topLeftCell=\"A5\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><cols><col min=\"1\" max=\"7\" width=\"28\" customWidth=\"1\"/></cols><sheetData>{rows}</sheetData>{(r.Grafico ? "<conditionalFormatting sqref=\"B5:B" + (r.Filas.Count + 3) + "\"><cfRule type=\"dataBar\" priority=\"1\"><dataBar><cfvo type=\"min\"/><cfvo type=\"max\"/><color rgb=\"FF174C40\"/></dataBar></cfRule></conditionalFormatting>" : "")}</worksheet>");
        }
        return ms.ToArray();
    }
}
public sealed class PdfExportador : IExportador
{
    public string Formato => "pdf"; public string Mime => "application/pdf";
    public byte[] Exportar(Reporte r)
    {
        // PDF con texto seleccionable, saltos de página y fuente estándar Latin-1.
        var lines = new List<string>();
        void Wrap(string text) { foreach (var original in text.Split('\n')) { var s = original.Replace('\r', ' '); while (s.Length > 105) { var cut = s.LastIndexOf(' ', 105); if (cut < 35) cut = 105; lines.Add(s[..cut]); s = s[cut..].TrimStart(); } lines.Add(s); } }
        Wrap(r.Filtros); Wrap($"Casos: {r.TotalCasos} | Filas: {r.Filas.Count}"); lines.Add("");
        var graphOffset = lines.Count;
        var graphRows = r.Grafico ? r.Filas.Where(f => f[0]?.ToString() != "TOTAL").Take(8).ToArray() : [];
        if (graphRows.Length > 0) { lines.Add("Casos por establecimiento"); for (var i = 0; i < graphRows.Length * 2 + 1; i++) lines.Add(""); }
        foreach (var row in r.Filas) { for (var i = 0; i < r.Columnas.Length; i++) Wrap($"{r.Columnas[i]}: {TextoReporte.Valor(row[i])}"); lines.Add(""); }
        var pages = lines.Chunk(43).ToArray(); if (pages.Length == 0) pages = [[]];
        var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", "", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" };
        string Hex(string s) => "<" + Convert.ToHexString(Encoding.Latin1.GetBytes(s.Replace('–', '-').Replace('→', '>'))) + ">";
        for (int p = 0; p < pages.Length; p++) {
            var stream = new StringBuilder($"BT /F1 16 Tf 40 800 Td {Hex(r.Titulo)} Tj /F1 9 Tf 0 -20 Td {Hex($"GIATEP | Emisión: {DateTimeOffset.Now:yyyy-MM-dd HH:mm} | Página {p + 1} de {pages.Length}")} Tj 0 -25 Td /F1 10 Tf 15 TL ");
            foreach (var line in pages[p]) stream.Append($"{Hex(line)} Tj T* "); stream.Append("ET");
            if (p == 0 && graphRows.Length > 0) {
                var max = Math.Max(1, graphRows.Max(f => Convert.ToInt32(f[1])));
                for (var j = 0; j < graphRows.Length; j++) {
                    var y = 755 - (graphOffset + 2) * 15 - j * 30;
                    var label = TextoReporte.Valor(graphRows[j][0]); if (label.Length > 40) label = label[..40];
                    var count = Convert.ToInt32(graphRows[j][1]);
                    stream.Append($" BT /F1 9 Tf 45 {y} Td {Hex(label + ": " + count)} Tj ET 0.09 0.30 0.25 rg 45 {y - 14} {(count * 450.0 / max).ToString("0.##", CultureInfo.InvariantCulture)} 8 re f 0 0 0 rg ");
                }
            }
            var pageId = objects.Count + 1; var contentId = pageId + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
            objects.Add($"<< /Length {Encoding.Latin1.GetByteCount(stream.ToString())} >>\nstream\n{stream}\nendstream");
        }
        objects[1] = $"<< /Type /Pages /Count {pages.Length} /Kids [{string.Join(' ', Enumerable.Range(0, pages.Length).Select(i => $"{4 + i * 2} 0 R"))}] >>";
        using var ms = new MemoryStream(); void Write(string s) => ms.Write(Encoding.Latin1.GetBytes(s));
        Write("%PDF-1.4\n"); var offsets = new List<long> { 0 }; for (int i = 0; i < objects.Count; i++) { offsets.Add(ms.Position); Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = ms.Position; Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n"); foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n"); Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); return ms.ToArray();
    }
}
