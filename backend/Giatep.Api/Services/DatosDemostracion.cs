using System.Text.Json;
using Giatep.Api.Models;
using Giatep.Api.ValidacionesDTO;
using Microsoft.EntityFrameworkCore;
namespace Giatep.Api.Services;

// Conjunto sintético identificable y reproducible. No contiene personas ni empresas reales.
public static class DatosDemostracion
{
    public const string Prefijo = "RUB26-";
    public const int TotalCasos = 600;
    private sealed record Escenario(string Actividad, string Lugar, string Relato, string Condicion, string Organizacion, string Intermedio, string Resultado, string Medida1, string Medida2, string Parte);
    private static readonly Escenario[] Accidentes = [
        new("Traslado de insumos", "Pasillo de abastecimiento", "La persona resbaló en una superficie húmeda durante el traslado de insumos y presentó una contusión de rodilla.", "El piso permanecía húmedo después de la limpieza", "El procedimiento no exigía delimitar el sector hasta su secado", "La persona ingresó al tramo húmedo durante el traslado", "Resbalón con contusión de rodilla", "Aplicar una verificación de secado antes de habilitar el pasillo", "Instalar señalización temporal y delimitar el sector durante la limpieza", "Rodilla"),
        new("Organización de materiales", "Bodega de insumos", "Un carro de traslado contactó materiales que sobresalían del área de almacenamiento y produjo un golpe en el antebrazo.", "Los materiales sobresalían del límite de almacenamiento", "No existía una revisión del despeje de las vías de circulación", "El carro contactó materiales durante su desplazamiento", "Golpe con contusión de antebrazo", "Reubicar los materiales dentro de los límites de almacenamiento", "Demarcar la circulación y registrar una inspección semanal de despeje", "Antebrazo"),
        new("Recepción de suministros", "Área de recepción", "Durante el movimiento manual de una caja de suministros se presentó dolor lumbar asociado al esfuerzo realizado.", "La caja se manipuló sin carro de apoyo disponible", "La planificación de la recepción no incluía una ayuda de traslado", "Se realizó el levantamiento manual de la carga", "Sobreesfuerzo con dolor lumbar", "Disponer un carro de traslado en el punto de recepción", "Revisar el procedimiento de recepción y practicar su ejecución con ayudas", "Zona lumbar"),
        new("Apertura de embalajes", "Puesto de preparación", "Una herramienta de apertura se deslizó durante el desembalaje y produjo un corte superficial en la mano.", "La herramienta de apertura carecía de protección retráctil", "La inspección de herramientas no estaba incorporada a la preparación", "El filo quedó expuesto durante la apertura del embalaje", "Corte superficial en la mano", "Sustituir la herramienta por un cortador con protección retráctil", "Incluir revisión de herramientas y retiro de unidades deterioradas", "Mano")];
    private static readonly Escenario[] Enfermedades = [
        new("Registro y procesamiento repetitivo de información", "Puesto administrativo", "La persona comunica molestias de muñeca al realizar tareas repetitivas. El caso simulado requiere investigar las condiciones del puesto y su exposición.", "El registro de información exigía movimientos repetidos de muñeca", "La programación no incluía alternancia de tareas ni pausas", "Se mantuvo una exposición repetitiva durante la jornada", "Molestias persistentes en muñeca comunicadas por la persona", "Ajustar el puesto y verificar la posición de teclado y apoyo", "Programar alternancia de tareas y pausas verificables", "Muñeca"),
        new("Preparación y uso de productos de limpieza", "Sector de servicios generales", "La persona comunica irritación respiratoria durante tareas de limpieza en un recinto con ventilación limitada. Se investigan los agentes y procedimientos del caso simulado.", "Se utilizaban productos de limpieza en un recinto poco ventilado", "La preparación del producto no incluía verificación de ventilación", "Se produjo exposición reiterada a vapores durante la limpieza", "Molestias respiratorias comunicadas durante la actividad", "Revisar la ventilación del recinto antes de ejecutar la limpieza", "Estandarizar la preparación y verificar la dilución de los productos", "Vías respiratorias"),
        new("Revisión de documentos y atención de solicitudes", "Estación de trabajo", "La persona comunica molestias cervicales durante actividades prolongadas frente a una pantalla. El escenario simulado permite revisar la postura y organización del trabajo.", "La pantalla estaba ubicada por debajo de la altura de referencia", "El puesto no contaba con una verificación de ajuste individual", "Se sostuvo una postura cervical fija durante tareas prolongadas", "Molestias cervicales persistentes comunicadas por la persona", "Regular la altura de pantalla y verificar el ajuste del puesto", "Implementar una pauta de ajuste y alternancia de actividades", "Cuello")];
    private static readonly Escenario Trayecto = new("Desplazamiento peatonal al trabajo", "Ruta peatonal de demostración", "Durante el trayecto peatonal desde el domicilio al trabajo, la persona tropezó en un desnivel de la ruta simulada y presentó una contusión de tobillo.", "La ruta peatonal tenía un desnivel visible", "No se había difundido una ruta alternativa ante la condición reportada", "La persona transitó por el tramo con desnivel", "Tropiezo de trayecto con contusión de tobillo", "Registrar el punto reportado y comunicarlo al responsable de infraestructura", "Difundir alternativas de desplazamiento seguro y verificar su recepción", "Tobillo");

    public static string RutDePrueba(int numero)
    {
        var texto = numero.ToString(); var suma = 0; var factor = 2;
        for (var i = texto.Length - 1; i >= 0; i--) { suma += (texto[i] - '0') * factor; factor = factor == 7 ? 2 : factor + 1; }
        var dv = 11 - suma % 11;
        return texto + (dv == 11 ? "0" : dv == 10 ? "K" : dv.ToString());
    }
    private static DateTimeOffset FechaHora(DateOnly dia, int hora) => new(TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(new TimeOnly(hora, 0)), TimeZoneInfo.FindSystemTimeZoneById("Pacific SA Standard Time")));

    public static void Cargar(GiatepDbContext db)
    {
        if (!db.Database.IsSqlServer()) { CargarDatos(db); return; }
        // Contexto exclusivo para la importación: evita MERGE de filas anchas y sus grandes concesiones de memoria.
        var options = new DbContextOptionsBuilder<GiatepDbContext>()
            .UseSqlServer(db.Database.GetDbConnection(), sql => sql.MaxBatchSize(1).CommandTimeout(120)).Options;
        using var carga = new GiatepDbContext(options);
        CargarDatos(carga);
    }
    private static void CargarDatos(GiatepDbContext db)
    {
        var existentes = db.Casos.Count(c => c.Codigo.StartsWith(Prefijo));
        if (existentes == TotalCasos) return;
        if (existentes != 0) throw new InvalidOperationException("Existe una carga parcial RUB26. Revisa sus datos antes de volver a cargar.");
        var centros = db.Establecimientos.Where(e => e.Activo).OrderBy(e => e.Nombre).Take(3).ToArray();
        var prevencion = db.Usuarios.First(u => u.Activo && u.Rol == "Prevencion");
        if (centros.Length < 3) throw new InvalidOperationException("La demostración necesita tres establecimientos activos.");
        var jefaturas = db.Usuarios.Where(u => u.Activo && u.Rol == "Jefatura").ToArray();
        var hoy = Consultas.Hoy(); var ahora = DateTimeOffset.UtcNow; var inicio = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-11);
        using var transaccion = db.Database.IsRelational() ? db.Database.BeginTransaction() : null;
        var nombresEmpresas = new[] { "Red Asistencial Horizonte", "Servicios Clínicos Austral", "Apoyo Sanitario Cordillera", "Mantención Biomédica del Valle", "Gestión Administrativa Los Robles", "Servicios Generales Buen Aire" };
        var empleadores = nombresEmpresas.Select((n, i) => new Empleador { Id = Guid.NewGuid(), Rut = RutDePrueba(90000001 + i), Nombre = "Demo · " + n, Direccion = $"Avenida Simulada {100 + i * 20}", Comuna = "Comuna de demostración", Telefono = "000000000", ActividadEconomica = i == 3 ? "Mantención de equipamiento de salud" : "Servicios de apoyo a establecimientos de salud", TrabajadoresHombres = 250, TrabajadorasMujeres = 250, Propiedad = i < 2 ? "Pública" : "Privada", TipoEmpresa = i < 3 ? "Principal" : "Contratista", ActividadPrincipal = i < 3 ? null : "Servicios de salud" }).ToArray();
        db.Empleadores.AddRange(empleadores);
        var nombres = new[] { "Emilia", "Tomás", "Antonia", "Matías", "Florencia", "Vicente", "Josefa", "Benjamín", "Catalina", "Joaquín", "Isidora", "Lucas", "Valentina", "Gabriel", "Amanda" };
        var apellidos = new[] { "Rojas", "Vega", "Araya", "Soto", "Fuentes", "Molina", "Tapia", "Vera", "Salas", "Torres", "Muñoz", "Silva", "Pérez", "Leiva", "Castro", "Bravo", "Díaz", "Lagos", "Reyes", "Pinto" };
        var cargos = new[] { "Administrativo de admisión", "Técnico de enfermería", "Auxiliar de servicios", "Encargado de bodega", "Técnico de mantención", "Apoyo clínico" };
        var trabajadores = Enumerable.Range(0, 600).Select(i => new Trabajador { Id = Guid.NewGuid(), Codigo = $"RUB26-T{i + 1:000}", Nombre = $"Demo · {nombres[i % 15]} {apellidos[i / 15 % 20]} {apellidos[(i / 300 + i / 15 + 2) % 20]}", Cargo = cargos[i % 6], EstablecimientoId = centros[i % 10 < 5 ? 0 : i % 10 < 8 ? 1 : 2].Id, Run = RutDePrueba(50000001 + i), Direccion = $"Pasaje Ficticio {200 + i}", Comuna = "Comuna de demostración", Telefono = "000000000", Sexo = i % 2 == 0 ? "Mujer" : "Hombre", FechaNacimiento = new DateOnly(1970 + i % 30, 1 + i % 12, 1 + i % 27), Nacionalidad = "Chilena", PuebloOriginario = "Ninguno" }).ToArray();
        db.Trabajadores.AddRange(trabajadores);
        foreach (var e in db.Empleadores.Where(e => e.Nombre == "Antecedente sintético para prueba del formulario")) e.Nombre = "Pruebas previas · empleador sintético";
        db.SaveChanges();
        var random = new Random(261103); var cantidades = new[] { 62, 58, 54, 50, 48, 46, 44, 46, 50, 52, 48, 42 }; var numero = 0;
        for (var mes = 0; mes < 12; mes++) for (var k = 0; k < cantidades[mes]; k++)
        {
            numero++; var trabajador = trabajadores[(numero - 1) * 37 % trabajadores.Length]; var wi = Array.IndexOf(trabajadores, trabajador); var empleador = empleadores[wi / 100]; var centro = centros.Single(c => c.Id == trabajador.EstablecimientoId);
            var primerDia = inicio.AddMonths(mes); var maximo = mes == 11 ? Math.Max(1, hoy.Day - 2) : DateTime.DaysInMonth(primerDia.Year, primerDia.Month); var fecha = primerDia.AddDays(random.Next(maximo));
            var tipo = numero % 10 < 3 ? TipoCaso.EnfermedadProfesional : TipoCaso.Accidente;
            var trayecto = tipo == TipoCaso.Accidente && numero % 7 == 0;
            var escenario = tipo == TipoCaso.EnfermedadProfesional ? Enfermedades[wi % Enfermedades.Length] : trayecto ? Trayecto : Accidentes[wi % Accidentes.Length];
            var sorteo = random.Next(100); var fase = mes < 8 && sorteo >= 15 ? 4 : sorteo < 15 ? 0 : sorteo < 40 ? 1 : sorteo < 55 ? 2 : sorteo < 80 ? 3 : 4;
            var estado = fase == 0 ? EstadoCaso.Borrador : fase <= 2 ? EstadoCaso.EnInvestigacion : fase == 3 ? EstadoCaso.EnSeguimiento : EstadoCaso.Cerrado;
            var registro = tipo == TipoCaso.EnfermedadProfesional ? fecha.AddDays(Math.Min(30, hoy.DayNumber - fecha.DayNumber)) : fecha;
            var creado = FechaHora(registro, 8); if (creado > ahora) creado = ahora.AddMinutes(-30);
            var demora = Math.Max(0, Math.Min(10 + numero % 12, hoy.DayNumber - registro.DayNumber));
            var terminado = FechaHora(registro.AddDays(demora), 16); if (terminado > ahora) terminado = ahora;
            var caso = new CasoRegistro { Id = Guid.NewGuid(), Codigo = $"{Prefijo}{numero:0000}", Tipo = tipo, EstablecimientoId = centro.Id, Establecimiento = centro.Nombre, TrabajadorId = trabajador.Id, Fecha = fecha, Relato = "Demostración sintética: " + escenario.Relato, Estado = estado, CreadoEn = creado, CerradoEn = fase == 4 ? terminado : null };
            db.Casos.Add(caso);
            var denuncia = CrearDenuncia(caso, trabajador, empleador, escenario, trayecto, hoy);
            DenunciaAlmacen.Guardar(db, caso, Denuncias.Validar(denuncia, tipo, fecha)!);
            if (fase != 0) AgregarInvestigacion(db, caso, trabajador, escenario, fase, numero, creado, terminado, prevencion, jefaturas.FirstOrDefault(j => j.EstablecimientoId == centro.Id) ?? prevencion);
            if (numero % 5 == 0) { db.SaveChanges(); db.ChangeTracker.Clear(); if (numero % 100 == 0) Console.WriteLine($"Datos de demostración: {numero}/{TotalCasos} casos preparados."); }
        }
        db.SaveChanges(); transaccion?.Commit();
    }

    private static DenunciaDTO CrearDenuncia(CasoRegistro caso, Trabajador t, Empleador e, Escenario s, bool trayecto, DateOnly hoy)
    {
        var d = new Dictionary<string, string> {
            ["fechaEmision"] = (caso.Tipo == TipoCaso.EnfermedadProfesional ? caso.Fecha.AddDays(Math.Min(30, hoy.DayNumber - caso.Fecha.DayNumber)) : caso.Fecha).ToString("yyyy-MM-dd"), ["empleadorNombre"] = e.Nombre, ["empleadorRut"] = e.Rut, ["empleadorDireccion"] = e.Direccion, ["empleadorComuna"] = e.Comuna, ["empleadorTelefono"] = "000000000", ["actividadEconomica"] = e.ActividadEconomica!, ["trabajadoresHombres"] = e.TrabajadoresHombres!.Value.ToString(), ["trabajadorasMujeres"] = e.TrabajadorasMujeres!.Value.ToString(), ["propiedad"] = e.Propiedad!, ["tipoEmpresa"] = e.TipoEmpresa!,
            ["trabajadorNombre"] = t.Nombre, ["trabajadorRun"] = t.Run!, ["trabajadorDireccion"] = t.Direccion!, ["trabajadorComuna"] = t.Comuna!, ["trabajadorTelefono"] = "000000000", ["sexo"] = t.Sexo!, ["fechaNacimiento"] = t.FechaNacimiento!.Value.ToString("yyyy-MM-dd"), ["nacionalidad"] = "Chilena", ["profesion"] = t.Cargo, ["antiguedad"] = "24", ["antiguedadUnidad"] = "Meses", ["tipoContrato"] = "Indefinido", ["tipoIngreso"] = "Remuneración fija", ["categoriaOcupacional"] = "Trabajador dependiente", ["puebloOriginario"] = "Ninguno",
            ["denuncianteNombre"] = "Demo · Representante de prevención", ["denuncianteRun"] = RutDePrueba(50000999), ["denuncianteTelefono"] = "000000000", ["denuncianteClasificacion"] = "Empleador", ["firmaNombre"] = "Documento de demostración sin firma" };
        if (e.ActividadPrincipal is not null) d["actividadPrincipal"] = e.ActividadPrincipal;
        if (caso.Tipo == TipoCaso.Accidente) {
            d["horaAccidente"] = "08:00"; d["horaIngreso"] = trayecto ? "09:00" : "07:30"; d["horaSalida"] = "17:00"; d["accidenteDireccion"] = trayecto ? "Ruta Ficticia 100" : e.Direccion; d["accidenteComuna"] = e.Comuna; d["actividadAccidente"] = s.Actividad; d["lugarAccidente"] = s.Lugar; d["trabajoHabitual"] = t.Cargo; d["realizabaHabitual"] = trayecto ? "No" : "Sí"; d["clasificacionAccidente"] = "Otro"; d["tipoAccidente"] = trayecto ? "Trayecto" : "Trabajo";
            if (trayecto) { d["tipoTrayecto"] = "Domicilio - Trabajo"; d["medioPrueba"] = "Declaración"; d["detallePrueba"] = "Declaración sintética del desplazamiento habitual en la ruta de demostración."; }
        } else {
            d["duracionSintomas"] = Math.Min(30, hoy.DayNumber - caso.Fecha.DayNumber).ToString(); d["duracionSintomasUnidad"] = "Días"; d["molestiasPrevias"] = "No"; d["parteCuerpo"] = s.Parte; d["actividadSintomas"] = s.Actividad; d["puestoSintomas"] = t.Cargo; d["companerosSintomas"] = "No"; d["agentes"] = s.Condicion; d["tiempoExposicion"] = "12"; d["tiempoExposicionUnidad"] = "Meses";
        }
        return new(caso.Tipo == TipoCaso.Accidente ? "DIAT" : "DIEP", d);
    }
    private static void AgregarInvestigacion(GiatepDbContext db, CasoRegistro caso, Trabajador t, Escenario s, int fase, int numero, DateTimeOffset creado, DateTimeOffset terminado, Usuario prevencion, Usuario jefe)
    {
        var duracion = (terminado - creado).Ticks;
        var inicio = creado.AddTicks(Math.Min(TimeSpan.FromMinutes(15).Ticks, duracion / 4));
        var cierre = creado.AddTicks(duracion * 3 / 4); var plan = creado.AddTicks(duracion * 7 / 8);
        var inv = new Investigacion { Id = Guid.NewGuid(), CasoId = caso.Id, Estado = fase >= 2 ? "Cerrada" : "EnCurso", IniciadaEn = inicio, ActualizadaEn = fase >= 3 ? plan : fase == 2 ? cierre : inicio, CerradaEn = fase >= 2 ? cierre : null, PlanIniciadoEn = fase >= 3 ? plan : null,
            Antecedentes = JsonSerializer.Serialize(new { codigo = caso.Codigo, tipo = caso.Tipo.ToString(), establecimiento = caso.Establecimiento, establecimientoId = caso.EstablecimientoId, trabajador = t.Nombre, fecha = caso.Fecha, relatoInicial = caso.Relato, denuncia = DenunciaAlmacen.Obtener(caso) }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            RelatoFinal = fase >= 2 || numero % 2 == 0 ? $"Reconstrucción sintética: {s.Relato} La entrevista y el acta de inspección de demostración respaldan las condiciones: {s.Condicion.ToLowerInvariant()}; {s.Organizacion.ToLowerInvariant()}. Se proponen controles vinculados a los hechos del árbol." : "" };
        db.Investigaciones.Add(inv);
        db.Entrevistas.Add(new() { Id = Guid.NewGuid(), InvestigacionId = inv.Id, Entrevistado = t.Nombre, Calidad = "Trabajador", Fecha = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(inicio, "Pacific SA Standard Time").DateTime), Declaracion = $"Entrevista ficticia de demostración. La actividad era: {s.Actividad}. La persona señala: {s.Relato}" });
        if (fase >= 2) db.Entrevistas.Add(new() { Id = Guid.NewGuid(), InvestigacionId = inv.Id, Entrevistado = "Demo · Testigo del sector", Calidad = "Testigo", Fecha = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(inicio, "Pacific SA Standard Time").DateTime), Declaracion = $"Declaración ficticia: se revisaron las condiciones del sector y se observó lo siguiente: {s.Condicion}. También se verificó: {s.Organizacion}." });
        var hechos = new[] { s.Condicion, s.Organizacion, s.Intermedio, s.Resultado }.Select((texto, i) => new Hecho { Id = Guid.NewGuid(), InvestigacionId = inv.Id, Descripcion = texto, EsFinal = i == 3 }).ToArray(); db.Hechos.AddRange(hechos);
        foreach (var (a, b) in new[] { (0, 2), (1, 2), (2, 3) }) db.RelacionesCausales.Add(new() { Id = Guid.NewGuid(), InvestigacionId = inv.Id, AntecedenteId = hechos[a].Id, ConsecuenciaId = hechos[b].Id });
        var evidencia = new PdfExportador().Exportar(new Reporte("Acta sintética de inspección", $"Demostración académica. Caso {caso.Codigo}. Fecha del evento: {caso.Fecha:yyyy-MM-dd}. No corresponde a una investigación real.", ["Antecedente", "Descripción"], [new object?[] { "Actividad", s.Actividad }, new object?[] { "Hallazgo del entorno", s.Condicion }, new object?[] { "Hallazgo de organización", s.Organizacion }], 1, false));
        db.Evidencias.Add(new() { Id = Guid.NewGuid(), CasoId = caso.Id, InvestigacionId = inv.Id, Nombre = $"acta-demo-{caso.Codigo}.pdf", Tipo = "application/pdf", Contenido = evidencia, UsuarioId = prevencion.Id, Fecha = inicio });
        var basePlazo = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(fase >= 3 ? plan : fase == 2 ? cierre : inicio, "Pacific SA Standard Time").DateTime);
        for (var i = 0; i < 2; i++) {
            var responsable = i == 0 ? jefe : prevencion; var propuesta = new MedidaInvestigacion { Id = Guid.NewGuid(), InvestigacionId = inv.Id, HechoId = hechos[i].Id, Descripcion = i == 0 ? s.Medida1 : s.Medida2, Responsable = responsable.Nombre, UsuarioResponsableId = responsable.Id, Plazo = basePlazo.AddDays(14 + i * 7 + numero % 14) }; db.MedidasInvestigacion.Add(propuesta);
            if (fase >= 3) db.Medidas.Add(new() { Id = Guid.NewGuid(), CasoId = caso.Id, PropuestaId = propuesta.Id, Activa = true, Descripcion = propuesta.Descripcion, Responsable = propuesta.Responsable, UsuarioResponsableId = responsable.Id, Plazo = propuesta.Plazo, Completada = fase == 4 || i == 0 && numero % 3 == 0 });
        }
    }
}
