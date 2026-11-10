using System.IO.Compression;
using System.Xml.Linq;
using Giatep.Api.Services;
using Xunit;
namespace Giatep.Tests;
public class DistribucionOcrTests
{
    [Fact]
    public void WordIncorporaOriginalYRecortaGraficosSinRedibujarlos()
    {
        var image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j7l8AAAAASUVORK5CYII=");
        var layout = new DistribucionOcr(1000,1400,[new("Texto revisado",100,400,700,20)]);
        var bytes = DocumentoEvidencia.Crear("Prueba", "Texto revisado", "original.png", Guid.NewGuid(), "Prueba", layout, [new("Logo", .1,.1,.2,.1),new("Firma",.2,.4,.3,.1)], image);
        using var zip = new ZipArchive(new MemoryStream(bytes));
        Assert.NotNull(zip.GetEntry("word/media/original.png"));
        using var input = zip.GetEntry("word/media/original.png")!.Open(); using var output = new MemoryStream(); input.CopyTo(output); Assert.Equal(image,output.ToArray());
        using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
        var xml=XDocument.Parse(reader.ReadToEnd());
        XNamespace wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
        XNamespace a="http://schemas.openxmlformats.org/drawingml/2006/main";
        Assert.Equal(2,xml.Descendants(wp+"anchor").Count());
        Assert.Equal(10000,(int)xml.Descendants(a+"srcRect").First().Attribute("l")!);
        Assert.NotNull(zip.GetEntry("word/_rels/document.xml.rels"));
    }
    [Fact]
    public void RecorteFueraDeImagenSeRechaza()
    {
        var layout = new DistribucionOcr(1000,1400,[new("Texto",100,400,700,20)]);
        Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(()=>DocumentoEvidencia.Crear("Prueba","Texto","a.png",null,"Prueba",layout,[new("Logo",.9,.1,.2,.1)],[137,80,78,71,13,10,26,10]));
    }
    [Fact]
    public void WordConservaPosicionesYTextoCorregidoEnMarcosEditables()
    {
        var layout = new DistribucionOcr(1000,1400,[new("Título corregido",400,100,200,30),new("Texto del cuerpo",100,400,700,20)]);
        var bytes = DocumentoEvidencia.Crear("Prueba", "Título corregido\nTexto del cuerpo", "original.png", Guid.NewGuid(), "Prueba", layout);
        using var zip = new ZipArchive(new MemoryStream(bytes));
        using var reader = new StreamReader(zip.GetEntry("word/document.xml")!.Open());
        var xml = XDocument.Parse(reader.ReadToEnd()); XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var frames = xml.Descendants(w+"framePr").ToArray();
        Assert.Equal(2,frames.Length);
        Assert.True((int)frames[0].Attribute(w+"x")! > (int)frames[1].Attribute(w+"x")!);
        Assert.True((int)frames[0].Attribute(w+"y")! < (int)frames[1].Attribute(w+"y")!);
        Assert.Contains("Título corregido",xml.ToString());
        Assert.Contains(xml.Descendants(w+"br"),b=>(string?)b.Attribute(w+"type")=="page");
    }
    [Fact]
    public void CoordenadasInvalidasNoGeneranWord()
    {
        Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => DocumentoEvidencia.ValidarDistribucion(new(100,100,[new("Texto",-1,0,20,10)])));
        Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => DocumentoEvidencia.ValidarDistribucion(new(100,100,[new("Texto",90,0,20,10)])));
    }
}
