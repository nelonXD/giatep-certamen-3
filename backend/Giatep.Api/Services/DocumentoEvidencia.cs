using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Giatep.Api.Services;

public static class DocumentoEvidencia
{
    public const string Mime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public static byte[] Crear(string titulo, string texto, string fuente, Guid? fuenteId, string autor)
    {
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Parrafo(string value) => new(w + "p", new XElement(w + "r", new XElement(w + "t",
            new XAttribute(XNamespace.Xml + "space", "preserve"), new string(value.Where(XmlConvert.IsXmlChar).ToArray()))));
        var body = new XElement(w + "body",
            Parrafo(titulo), Parrafo("Transcripción de evidencia · GIATEP"),
            Parrafo(fuenteId.HasValue ? $"Imagen original: {fuente} ({fuenteId})" : $"Imagen utilizada: {fuente}. Procesada temporalmente; no almacenada como evidencia."),
            Parrafo($"Revisado y guardado por: {autor} · {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC"),
            Parrafo("Texto obtenido mediante OCR local y revisado por el usuario. No constituye una conclusión de la investigación."),
            texto.Replace("\r", "").Split('\n').Select(Parrafo),
            new XElement(w + "sectPr", new XElement(w + "pgSz", new XAttribute(w + "w", "11906"), new XAttribute(w + "h", "16838"))));
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Entry(string name, string content) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(content); }
            Entry("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Entry("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            Entry("word/document.xml", new XDocument(new XElement(w + "document", new XAttribute(XNamespace.Xmlns + "w", w), body)).ToString());
        }
        return output.ToArray();
    }
}
