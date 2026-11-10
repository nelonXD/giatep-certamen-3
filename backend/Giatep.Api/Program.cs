using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Giatep.Api.Endpoints;
using Giatep.Api.Repositories;
using Giatep.Api.Services;
using Giatep.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
Inicializacion.CargarEntorno(builder);
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, context, token) => {
    document.Info.Title = "GIATEP API";
    document.Info.Version = "v1";
    document.Info.Description = "Gestión preventiva con JWT y roles Administrador, Prevencion y Jefatura. Los errores usan Problem Details. Incluye OCR de evidencias mediante OCR.space (o Google Cloud Vision opcional) y generación de documentos Word revisados.";
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", Description = "Token de ocho horas emitido por /api/v1/auth/login." };
    foreach (var item in document.Paths.Values) if (item.Operations is not null) foreach (var operation in item.Operations.Values) {
        operation.Responses ??= new OpenApiResponses();
        foreach (var error in new[] { ("400", "Entrada inválida. Problem Details con title y status."), ("401", "Sesión ausente, inválida o expirada."), ("403", "Permisos insuficientes."), ("404", "Recurso no encontrado dentro del ámbito autorizado."), ("409", "Regla de negocio o integridad incumplida."), ("429", "Límite de solicitudes alcanzado."), ("500", "Error interno sin detalles sensibles.") })
            operation.Responses.TryAdd(error.Item1, new OpenApiResponse { Description = error.Item2, Content = new Dictionary<string, OpenApiMediaType> { ["application/problem+json"] = new() { Schema = new OpenApiSchema { Type = JsonSchemaType.Object, Properties = new Dictionary<string, IOpenApiSchema> { ["type"] = new OpenApiSchema { Type = JsonSchemaType.String }, ["title"] = new OpenApiSchema { Type = JsonSchemaType.String }, ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer }, ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String } } } } } });
    }
    document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
    foreach (var path in new[] { "/health", "/api/v1/auth/login" }) if (document.Paths.TryGetValue(path, out var item) && item.Operations is not null) foreach (var operation in item.Operations.Values) operation.Security = [];
    return Task.CompletedTask;
}));
builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var conexion = builder.Configuration.GetConnectionString("Giatep");
if (string.IsNullOrWhiteSpace(conexion))
    throw new InvalidOperationException("Configura ConnectionStrings__Giatep como variable de entorno o en .env para desarrollo local.");
builder.Services.AddDbContext<GiatepDbContext>(options => options.UseSqlServer(conexion));
builder.Services.AddSingleton(new AlmacenEvidencias(builder.Configuration["Storage:Path"] ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../storage/evidencias"))));
builder.Services.AddScoped<ICasoRepository, CasoSqlRepository>();
builder.Services.AddScoped<CasoService>();
builder.Services.AddScoped<InvestigacionService>();
builder.Services.AddScoped<AccesoService>();
builder.Services.AddScoped<AnalisisService>();
if (string.Equals(builder.Configuration["Ocr:Engine"] ?? "OcrSpace", "OcrSpace", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<IOcrLocalService, OcrSpaceService>().RedactLoggedHeaders(new[] { "apikey" });
else if (string.Equals(builder.Configuration["Ocr:Engine"], "GoogleVision", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<IOcrLocalService, OcrApiService>();
else builder.Services.AddScoped<IOcrLocalService, OcrLocalService>();
builder.Services.AddSingleton<IExportador, CsvExportador>();
builder.Services.AddSingleton<IExportador, ExcelExportador>();
builder.Services.AddSingleton<IExportador, PdfExportador>();
var key = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("Configura Jwt__Key con al menos 32 bytes en .env.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new() { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = "giatep", ValidAudience = "giatep-web", IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ClockSkew = TimeSpan.FromSeconds(10) };
    options.Events = new JwtBearerEvents {
        OnTokenValidated = async context => {
            var db = context.HttpContext.RequestServices.GetRequiredService<GiatepDbContext>();
            var id = context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = Guid.TryParse(id, out var parsed) ? await db.Usuarios.FindAsync(parsed) : null;
            var principal = context.Principal!;
            if (user is null || !user.Activo || user.Rol != principal.FindFirstValue(ClaimTypes.Role) || (user.EstablecimientoId?.ToString() ?? "") != (principal.FindFirstValue("establecimiento") ?? "") || principal.FindFirstValue("version") != Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash)))) context.Fail("La cuenta o sus permisos cambiaron.");
        },
        OnChallenge = async context => { context.HandleResponse(); await Results.Problem(statusCode: 401, title: "Inicia sesión para continuar.").ExecuteAsync(context.HttpContext); },
        OnForbidden = context => Results.Problem(statusCode: 403, title: "No tienes permisos para esta operación.").ExecuteAsync(context.HttpContext)
    };
});
builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("Administrador", p => p.RequireRole("Administrador"));
    options.AddPolicy("Prevencion", p => p.RequireRole("Prevencion"));
    options.AddPolicy("Operacion", p => p.RequireRole("Prevencion", "Jefatura"));
});
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() { PermitLimit = 240, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddFixedWindowLimiter("login", o => { o.PermitLimit = 20; o.Window = TimeSpan.FromMinutes(1); });
    options.AddFixedWindowLimiter("exportacion", o => { o.PermitLimit = 30; o.Window = TimeSpan.FromMinutes(1); });
    options.AddPolicy("ocr", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
        _ => new() { PermitLimit = 6, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = (context, token) => new ValueTask(Results.Problem(statusCode: 429, title: "Demasiadas solicitudes. Espera un minuto.").ExecuteAsync(context.HttpContext));
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 6 * 1024 * 1024);
var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (Exception ex) when (ex is ValidationException or KeyNotFoundException or ReglaNegocioException)
    {
        var status = ex is KeyNotFoundException ? 404 : ex is ReglaNegocioException ? 409 : 400;
        await Results.Problem(statusCode: status, title: ex.Message).ExecuteAsync(context);
    }
    catch (BadHttpRequestException) { await Results.Problem(statusCode: 400, title: "Solicitud inválida o demasiado grande.").ExecuteAsync(context); }
    catch (DbUpdateException ex) { app.Logger.LogWarning("Error de integridad de datos: {Tipo}", ex.GetType().Name); await Results.Problem(statusCode: 409, title: "Los datos incumplen una restricción o fueron modificados. Revisa e intenta nuevamente.").ExecuteAsync(context); }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al procesar la solicitud");
        await Results.Problem(statusCode: 500, title: "No se pudo completar la operación.").ExecuteAsync(context);
    }
});
app.Use(async (context, next) => {
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Cache-Control"] = "no-store";
    await next(context);
});
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapGet("/health", async (GiatepDbContext db, CancellationToken token) =>
    await db.Database.CanConnectAsync(token)
        ? Results.Ok(new { estado = "disponible", almacenamiento = "SQL Server" })
        : Results.Problem(statusCode: 503, title: "La base de datos no está disponible.")).AllowAnonymous();
if (app.Environment.IsDevelopment()) { app.MapOpenApi().AllowAnonymous(); app.MapScalarApiReference().AllowAnonymous(); }
app.MapCasos();
app.MapInvestigaciones();
app.MapEvidenciaOcr();
app.MapPlanes();
app.MapAuth();
app.MapGestion();
app.MapRegistrosRut();
app.MapReportes();
// Solo para el desarrollo local. En producción las migraciones serán un paso del despliegue.
if (app.Environment.IsDevelopment() || args.Contains("--migrar-archivos=true"))
{
    using var scope = app.Services.CreateScope();
    await MigradorArchivos.Migrar(scope.ServiceProvider.GetRequiredService<GiatepDbContext>());
    if (!args.Contains("--migrar-archivos=true")) Inicializacion.Sembrar(scope.ServiceProvider.GetRequiredService<GiatepDbContext>(), app.Configuration);
}
if (args.Contains("--migrar-archivos=true")) return;
app.Run();
public partial class Program { }
