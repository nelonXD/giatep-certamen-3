using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Giatep.Api.Services;

public record RecorteOcr(string Nombre, double X, double Y, double Ancho, double Alto);

public static class DocumentoEvidencia
{
    public static void ValidarDistribucion(DistribucionOcr d)
    {
        if (!double.IsFinite(d.Ancho) || !double.IsFinite(d.Alto) || d.Ancho <= 0 || d.Alto <= 0 || d.Ancho > 30000 || d.Alto > 30000 || d.Lineas is null || d.Lineas.Length is < 1 or > 1000)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Distribución del documento inválida.");
        if (d.Lineas.Any(l => l is null || l.Texto is null || l.Texto.Length > 2000 || !double.IsFinite(l.X) || !double.IsFinite(l.Y) || !double.IsFinite(l.Ancho) || !double.IsFinite(l.Alto) || l.X < 0 || l.Y < 0 || l.Ancho <= 0 || l.Alto <= 0 || l.X + l.Ancho > d.Ancho || l.Y + l.Alto > d.Alto) || d.Lineas.Sum(l => l.Texto.Length) > 30000)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Las líneas del documento son inválidas.");
    }
    public const string Mime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public static byte[] Crear(string titulo, string texto, string fuente, Guid? fuenteId, string autor, DistribucionOcr? distribucion = null, RecorteOcr[]? recortes = null, byte[]? original = null)
    {
        recortes ??= [];
        if (recortes.Length > 12 || recortes.Any(r => r is null || string.IsNullOrWhiteSpace(r.Nombre) || r.Nombre.Length > 80 || !double.IsFinite(r.X) || !double.IsFinite(r.Y) || !double.IsFinite(r.Ancho) || !double.IsFinite(r.Alto) || r.X < 0 || r.Y < 0 || r.Ancho <= 0 || r.Alto <= 0 || r.X+r.Ancho > 1.00001 || r.Y+r.Alto > 1.00001))
            throw new System.ComponentModel.DataAnnotations.ValidationException("Recortes inválidos. Selecciona hasta 12 zonas dentro de la imagen.");
        if (recortes.Length > 0 && (distribucion is null || original is null || original.Length < 8 || original.Length > 1_000_000))
            throw new System.ComponentModel.DataAnnotations.ValidationException("Los recortes necesitan la imagen original y su distribución.");
        var png = original is { Length: >= 8 } && original.AsSpan(0,8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        if (recortes.Length > 0 && !png && !(original![0] == 255 && original[1] == 216 && original[2] == 255))
            throw new System.ComponentModel.DataAnnotations.ValidationException("Imagen original inválida.");
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XElement Parrafo(string value) => new(w + "p", new XElement(w + "r", new XElement(w + "t",
            new XAttribute(XNamespace.Xml + "space", "preserve"), new string(value.Where(XmlConvert.IsXmlChar).ToArray()))));
        var body = new XElement(w + "body",
            Parrafo(titulo), Parrafo("Transcripción de evidencia · GIATEP"),
            Parrafo(fuenteId.HasValue ? $"Imagen original: {fuente} ({fuenteId})" : $"Imagen utilizada: {fuente}. Procesada temporalmente; no almacenada como evidencia."),
            Parrafo($"Revisado y guardado por: {autor} · {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC"),
            Parrafo("Texto obtenido mediante OCR y revisado por el usuario. No constituye una conclusión de la investigación."),
            texto.Replace("\r", "").Split('\n').Select(Parrafo),
            new XElement(w + "sectPr", new XElement(w + "pgSz", new XAttribute(w + "w", "11906"), new XAttribute(w + "h", "16838"))));
        if (distribucion is not null) {
            ValidarDistribucion(distribucion);
            const double pageWidth = 12240, pageHeight = 15840;
            var scale = Math.Min((pageWidth - 720) / distribucion.Ancho, (pageHeight - 720) / distribucion.Alto);
            var positioned = distribucion.Lineas.Select(line => {
                var p = Parrafo(line.Texto);
                p.AddFirst(new XElement(w + "pPr",
                    new XElement(w + "framePr", new XAttribute(w + "w", Math.Max(120, (int)(line.Ancho * scale + 100))),
                        new XAttribute(w + "x", (int)(360 + line.X * scale)), new XAttribute(w + "y", (int)(360 + line.Y * scale)),
                        new XAttribute(w + "hAnchor", "page"), new XAttribute(w + "vAnchor", "page"), new XAttribute(w + "wrap", "none")),
                    new XElement(w + "spacing", new XAttribute(w + "before", 0), new XAttribute(w + "after", 0))));
                p.Element(w + "r")!.AddFirst(new XElement(w + "rPr", new XElement(w + "sz", new XAttribute(w + "val", Math.Clamp((int)(line.Alto * scale / 10 * 0.8), 8, 72)))));
                return p;
            }).ToArray();
            XNamespace wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
            XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
            XNamespace pic = "http://schemas.openxmlformats.org/drawingml/2006/picture";
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var drawings = recortes.Select((crop, index) => {
                var cx = (long)(crop.Ancho * distribucion.Ancho * scale * 635);
                var cy = (long)(crop.Alto * distribucion.Alto * scale * 635);
                return new XElement(w + "p", new XElement(w + "r", new XElement(w + "drawing",
                    new XElement(wp + "anchor", new XAttribute("distT",0), new XAttribute("distB",0), new XAttribute("distL",0), new XAttribute("distR",0), new XAttribute("simplePos",0), new XAttribute("relativeHeight",251658240+index), new XAttribute("behindDoc",0), new XAttribute("locked",0), new XAttribute("layoutInCell",1), new XAttribute("allowOverlap",1),
                        new XElement(wp + "simplePos", new XAttribute("x",0), new XAttribute("y",0)),
                        new XElement(wp + "positionH",new XAttribute("relativeFrom","page"),new XElement(wp+"posOffset",(long)((360+crop.X*distribucion.Ancho*scale)*635))),
                        new XElement(wp + "positionV",new XAttribute("relativeFrom","page"),new XElement(wp+"posOffset",(long)((360+crop.Y*distribucion.Alto*scale)*635))),
                        new XElement(wp + "extent",new XAttribute("cx",cx),new XAttribute("cy",cy)), new XElement(wp+"wrapNone"),
                        new XElement(wp+"docPr",new XAttribute("id",index+1),new XAttribute("name",crop.Nombre),new XAttribute("descr","Recorte de la evidencia original")),
                        new XElement(wp+"cNvGraphicFramePr"),
                        new XElement(a+"graphic", new XElement(a+"graphicData",new XAttribute("uri",pic.NamespaceName),
                            new XElement(pic+"pic",new XElement(pic+"nvPicPr",new XElement(pic+"cNvPr",new XAttribute("id",index+1),new XAttribute("name",crop.Nombre)),new XElement(pic+"cNvPicPr")),
                                new XElement(pic+"blipFill",new XElement(a+"blip",new XAttribute(rel+"embed","rIdOriginal")),
                                    new XElement(a+"srcRect",new XAttribute("l",(int)(crop.X*100000)),new XAttribute("t",(int)(crop.Y*100000)),new XAttribute("r",Math.Max(0,(int)((1-crop.X-crop.Ancho)*100000))),new XAttribute("b",Math.Max(0,(int)((1-crop.Y-crop.Alto)*100000)))),
                                    new XElement(a+"stretch",new XElement(a+"fillRect"))),
                                new XElement(pic+"spPr",new XElement(a+"xfrm",new XElement(a+"off",new XAttribute("x",0),new XAttribute("y",0)),new XElement(a+"ext",new XAttribute("cx",cx),new XAttribute("cy",cy))),new XElement(a+"prstGeom",new XAttribute("prst","rect"),new XElement(a+"avLst"))))))))));
            }).ToArray();
            var metadata = body.Elements().Where(e => e.Name != w + "sectPr").Take(5).ToArray();
            body = new XElement(w + "body", drawings, positioned,
                new XElement(w + "p", new XElement(w + "r", new XElement(w + "br", new XAttribute(w + "type", "page")))),
                metadata, Parrafo("Distribución aproximada reconstruida a partir de las posiciones del OCR. Los elementos gráficos seleccionados son recortes de la evidencia original, sin reconstrucción ni validación de autenticidad."),
                new XElement(w + "sectPr", new XElement(w + "pgSz", new XAttribute(w + "w", (int)pageWidth), new XAttribute(w + "h", (int)pageHeight)),
                    new XElement(w + "pgMar", new XAttribute(w + "top", 360), new XAttribute(w + "bottom", 360), new XAttribute(w + "left", 360), new XAttribute(w + "right", 360))));
        }
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Entry(string name, string content) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(content); }
            Entry("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"png\" ContentType=\"image/png\"/><Default Extension=\"jpg\" ContentType=\"image/jpeg\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/></Types>");
            Entry("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/></Relationships>");
            if (recortes.Length > 0) {
                var imageName = png ? "original.png" : "original.jpg";
                using (var stream = zip.CreateEntry($"word/media/{imageName}").Open()) stream.Write(original!);
                XNamespace r = "http://schemas.openxmlformats.org/package/2006/relationships";
                Entry("word/_rels/document.xml.rels", new XElement(r+"Relationships", new XElement(r+"Relationship",new XAttribute("Id","rIdOriginal"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"),new XAttribute("Target",$"media/{imageName}"))).ToString());
            }
            Entry("word/document.xml", new XDocument(new XElement(w + "document", new XAttribute(XNamespace.Xmlns + "w", w), body)).ToString());
        }
        return output.ToArray();
    }
}
