using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Giatep.Tests;
public class PlanApiTests(GiatepFactory factory) : IClassFixture<GiatepFactory>
{
    private async Task<HttpClient> Login(string cuenta) {
        var c = factory.CreateClient(); var r = await c.PostAsJsonAsync("/api/v1/auth/login", new { cuenta, password = GiatepFactory.Password });
        r.EnsureSuccessStatusCode(); var sesion = await r.Content.ReadFromJsonAsync<JsonElement>(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString()); return c;
    }
    private static MultipartFormDataContent Entrega(bool valida = true) {
        var f = new MultipartFormDataContent(); f.Add(new StringContent("Implementación sintética documentada con evidencia de prueba."), "comentario");
        f.Add(new ByteArrayContent(valida ? Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jF5kAAAAASUVORK5CYII=") : "contenido falso"u8.ToArray()), "archivo", "prueba.png"); return f;
    }
    [Fact] public async Task EntregaRevisionDevolucionYPermisos() {
        using var c = await Login("prevencion"); using var jefe = await Login("jefatura"); using var otro = await Login("jefatura.sur"); using var admin = await Login("admin");
        Guid casoId, medidaId, ajenaId;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<GiatepDbContext>(); var usuario = db.Usuarios.Single(u => u.Cuenta == "jefatura"); var prev = db.Usuarios.Single(u => u.Cuenta == "prevencion");
            var caso = new CasoRegistro { Id = Guid.NewGuid(), Codigo = "PLAN-TEST-" + Guid.NewGuid().ToString("N")[..8], Estado = EstadoCaso.EnSeguimiento, EstablecimientoId = usuario.EstablecimientoId, Establecimiento = "Centro de prueba", Fecha = Consultas.Hoy(), CreadoEn = DateTimeOffset.UtcNow };
            var m = new MedidaRegistro { Id = Guid.NewGuid(), CasoId = caso.Id, Descripcion = "Medida preventiva de prueba", Responsable = usuario.Nombre, UsuarioResponsableId = usuario.Id, Plazo = Consultas.Hoy() };
            var ajena = new MedidaRegistro { Id = Guid.NewGuid(), CasoId = caso.Id, Descripcion = "Otra medida de prueba", Responsable = prev.Nombre, UsuarioResponsableId = prev.Id, Plazo = Consultas.Hoy() };
            db.Casos.Add(caso); db.Medidas.AddRange(m, ajena); db.Investigaciones.Add(new Investigacion { Id = Guid.NewGuid(), CasoId = caso.Id, Estado = "Cerrada", PlanIniciadoEn = DateTimeOffset.UtcNow }); db.SaveChanges(); casoId = caso.Id; medidaId = m.Id; ajenaId = ajena.Id;
        }
        var plan = $"/api/v1/planes/{casoId}"; var medida = plan + $"/medidas/{medidaId}";
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/v1/planes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync(plan)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await jefe.GetAsync(plan)).StatusCode);
        using (var f = Entrega()) Assert.Equal(HttpStatusCode.Forbidden, (await jefe.PostAsync(plan + $"/medidas/{ajenaId}/entrega", f)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync(medida + "/revision", new { aprobada = true, observacion = "Revisión antes de entrega inválida" })).StatusCode);
        using (var f = Entrega()) Assert.Equal(HttpStatusCode.Forbidden, (await jefe.PostAsync(medida + "/entrega", f)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jefe.PostAsJsonAsync($"/api/v1/casos/{casoId}/observaciones", new { texto = "Observación no autorizada de jefatura" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jefe.PutAsJsonAsync($"/api/v1/casos/{casoId}/medidas/{medidaId}/cumplimiento", new { completada = true })).StatusCode);
        using (var f = Entrega(false)) Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync(medida + "/entrega", f)).StatusCode);
        using (var f = Entrega()) (await c.PostAsync(medida + "/entrega", f)).EnsureSuccessStatusCode();
        using (var f = Entrega()) Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync(medida + "/entrega", f)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await jefe.PutAsJsonAsync(medida + "/revision", new { aprobada = true, observacion = "No puede verificar la jefatura" })).StatusCode);
        (await c.PutAsJsonAsync(medida + "/revision", new { aprobada = false, observacion = "Debe complementar la evidencia adjunta" })).EnsureSuccessStatusCode();
        using (var f = Entrega()) (await c.PostAsync(medida + "/entrega", f)).EnsureSuccessStatusCode();
        (await c.PutAsJsonAsync(medida + "/revision", new { aprobada = true, observacion = "Cumplimiento verificado con evidencia" })).EnsureSuccessStatusCode();
        var detalle = await c.GetFromJsonAsync<JsonElement>(plan); Assert.True(detalle.GetProperty("medidas").EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == medidaId).GetProperty("completada").GetBoolean());
        Assert.Equal(2, detalle.GetProperty("evidencias").GetArrayLength());
        var evidenciaId = detalle.GetProperty("evidencias")[0].GetProperty("id").GetString();
        (await jefe.GetAsync(plan + $"/evidencias/{evidenciaId}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync(plan + $"/evidencias/{evidenciaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/v1/casos/{casoId}/estado", new { estado = "Cerrado" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/planes?estado=EnSeguimiento")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/planes?estado=inventado")).StatusCode);
    }
}
