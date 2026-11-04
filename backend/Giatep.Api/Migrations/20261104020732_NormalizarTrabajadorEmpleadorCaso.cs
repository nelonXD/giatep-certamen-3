using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giatep.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizarTrabajadorEmpleadorCaso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comuna",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "Trabajadores",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nacionalidad",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtroPueblo",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PuebloOriginario",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Run",
                table: "Trabajadores",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sexo",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmpleadorId",
                table: "Casos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Empleadores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rut = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Comuna = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadEconomica = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadoresHombres = table.Column<int>(type: "int", nullable: true),
                    TrabajadorasMujeres = table.Column<int>(type: "int", nullable: true),
                    Propiedad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoEmpresa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadPrincipal = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DenunciasCasos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CasoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrabajadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Formulario = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: true),
                    CodigoOrganismo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Folio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorRut = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorDireccion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorComuna = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorTelefono = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadEconomica = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadoresHombres = table.Column<int>(type: "int", nullable: true),
                    TrabajadorasMujeres = table.Column<int>(type: "int", nullable: true),
                    Propiedad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoEmpresa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadPrincipal = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorRun = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorDireccion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorComuna = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorTelefono = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Sexo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Nacionalidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Profesion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Antiguedad = table.Column<int>(type: "int", nullable: true),
                    AntiguedadUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoContrato = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoIngreso = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CategoriaOcupacional = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PuebloOriginario = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OtroPueblo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HoraAccidente = table.Column<TimeOnly>(type: "time", nullable: true),
                    HoraIngreso = table.Column<TimeOnly>(type: "time", nullable: true),
                    HoraSalida = table.Column<TimeOnly>(type: "time", nullable: true),
                    AccidenteDireccion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AccidenteComuna = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LugarAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajoHabitual = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RealizabaHabitual = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClasificacionAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoTrayecto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MedioPrueba = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DetallePrueba = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DuracionSintomas = table.Column<int>(type: "int", nullable: true),
                    DuracionSintomasUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MolestiasPrevias = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ParteCuerpo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PuestoSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanerosSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Agentes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TiempoExposicion = table.Column<int>(type: "int", nullable: true),
                    TiempoExposicionUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteRun = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteTelefono = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteClasificacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FirmaNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenunciasCasos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DenunciasCasos_Casos_CasoId",
                        column: x => x.CasoId,
                        principalTable: "Casos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DenunciasCasos_Empleadores_EmpleadorId",
                        column: x => x.EmpleadorId,
                        principalTable: "Empleadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DenunciasCasos_Trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_Run",
                table: "Trabajadores",
                column: "Run",
                unique: true,
                filter: "[Run] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Casos_EmpleadorId",
                table: "Casos",
                column: "EmpleadorId");

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasCasos_CasoId",
                table: "DenunciasCasos",
                column: "CasoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasCasos_EmpleadorId",
                table: "DenunciasCasos",
                column: "EmpleadorId");

            migrationBuilder.CreateIndex(
                name: "IX_DenunciasCasos_TrabajadorId",
                table: "DenunciasCasos",
                column: "TrabajadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Empleadores_Rut",
                table: "Empleadores",
                column: "Rut",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Casos_Empleadores_EmpleadorId",
                table: "Casos",
                column: "EmpleadorId",
                principalTable: "Empleadores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            // Traslado transaccional; se conserva el JSON anterior como archivo de compatibilidad.
            migrationBuilder.Sql(@"SELECT c.[Id],
c.[TrabajadorId],
c.[CreadoEn],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Formulario'), JSON_VALUE(c.[DenunciaJson], '$.formulario')) AS [Formulario],
TRY_CONVERT(date, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.fechaEmision'), JSON_VALUE(c.[DenunciaJson], '$.datos.fechaEmision')), '')) AS [FechaEmision],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.codigoOrganismo'), JSON_VALUE(c.[DenunciaJson], '$.datos.codigoOrganismo')) AS [CodigoOrganismo],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.folio'), JSON_VALUE(c.[DenunciaJson], '$.datos.folio')) AS [Folio],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.empleadorNombre'), JSON_VALUE(c.[DenunciaJson], '$.datos.empleadorNombre')) AS [EmpleadorNombre],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.empleadorRut'), JSON_VALUE(c.[DenunciaJson], '$.datos.empleadorRut')) AS [EmpleadorRut],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.empleadorDireccion'), JSON_VALUE(c.[DenunciaJson], '$.datos.empleadorDireccion')) AS [EmpleadorDireccion],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.empleadorComuna'), JSON_VALUE(c.[DenunciaJson], '$.datos.empleadorComuna')) AS [EmpleadorComuna],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.empleadorTelefono'), JSON_VALUE(c.[DenunciaJson], '$.datos.empleadorTelefono')) AS [EmpleadorTelefono],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.actividadEconomica'), JSON_VALUE(c.[DenunciaJson], '$.datos.actividadEconomica')) AS [ActividadEconomica],
TRY_CONVERT(int, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadoresHombres'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadoresHombres')), '')) AS [TrabajadoresHombres],
TRY_CONVERT(int, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadorasMujeres'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadorasMujeres')), '')) AS [TrabajadorasMujeres],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.propiedad'), JSON_VALUE(c.[DenunciaJson], '$.datos.propiedad')) AS [Propiedad],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.tipoEmpresa'), JSON_VALUE(c.[DenunciaJson], '$.datos.tipoEmpresa')) AS [TipoEmpresa],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.actividadPrincipal'), JSON_VALUE(c.[DenunciaJson], '$.datos.actividadPrincipal')) AS [ActividadPrincipal],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadorNombre'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadorNombre')) AS [TrabajadorNombre],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadorRun'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadorRun')) AS [TrabajadorRun],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadorDireccion'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadorDireccion')) AS [TrabajadorDireccion],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadorComuna'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadorComuna')) AS [TrabajadorComuna],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadorTelefono'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadorTelefono')) AS [TrabajadorTelefono],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.sexo'), JSON_VALUE(c.[DenunciaJson], '$.datos.sexo')) AS [Sexo],
TRY_CONVERT(date, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.fechaNacimiento'), JSON_VALUE(c.[DenunciaJson], '$.datos.fechaNacimiento')), '')) AS [FechaNacimiento],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.nacionalidad'), JSON_VALUE(c.[DenunciaJson], '$.datos.nacionalidad')) AS [Nacionalidad],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.profesion'), JSON_VALUE(c.[DenunciaJson], '$.datos.profesion')) AS [Profesion],
TRY_CONVERT(int, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.antiguedad'), JSON_VALUE(c.[DenunciaJson], '$.datos.antiguedad')), '')) AS [Antiguedad],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.antiguedadUnidad'), JSON_VALUE(c.[DenunciaJson], '$.datos.antiguedadUnidad')) AS [AntiguedadUnidad],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.tipoContrato'), JSON_VALUE(c.[DenunciaJson], '$.datos.tipoContrato')) AS [TipoContrato],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.tipoIngreso'), JSON_VALUE(c.[DenunciaJson], '$.datos.tipoIngreso')) AS [TipoIngreso],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.categoriaOcupacional'), JSON_VALUE(c.[DenunciaJson], '$.datos.categoriaOcupacional')) AS [CategoriaOcupacional],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.puebloOriginario'), JSON_VALUE(c.[DenunciaJson], '$.datos.puebloOriginario')) AS [PuebloOriginario],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.otroPueblo'), JSON_VALUE(c.[DenunciaJson], '$.datos.otroPueblo')) AS [OtroPueblo],
TRY_CONVERT(time, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.horaAccidente'), JSON_VALUE(c.[DenunciaJson], '$.datos.horaAccidente')), '')) AS [HoraAccidente],
TRY_CONVERT(time, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.horaIngreso'), JSON_VALUE(c.[DenunciaJson], '$.datos.horaIngreso')), '')) AS [HoraIngreso],
TRY_CONVERT(time, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.horaSalida'), JSON_VALUE(c.[DenunciaJson], '$.datos.horaSalida')), '')) AS [HoraSalida],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.accidenteDireccion'), JSON_VALUE(c.[DenunciaJson], '$.datos.accidenteDireccion')) AS [AccidenteDireccion],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.accidenteComuna'), JSON_VALUE(c.[DenunciaJson], '$.datos.accidenteComuna')) AS [AccidenteComuna],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.actividadAccidente'), JSON_VALUE(c.[DenunciaJson], '$.datos.actividadAccidente')) AS [ActividadAccidente],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.lugarAccidente'), JSON_VALUE(c.[DenunciaJson], '$.datos.lugarAccidente')) AS [LugarAccidente],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajoHabitual'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajoHabitual')) AS [TrabajoHabitual],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.realizabaHabitual'), JSON_VALUE(c.[DenunciaJson], '$.datos.realizabaHabitual')) AS [RealizabaHabitual],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.clasificacionAccidente'), JSON_VALUE(c.[DenunciaJson], '$.datos.clasificacionAccidente')) AS [ClasificacionAccidente],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.tipoAccidente'), JSON_VALUE(c.[DenunciaJson], '$.datos.tipoAccidente')) AS [TipoAccidente],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.tipoTrayecto'), JSON_VALUE(c.[DenunciaJson], '$.datos.tipoTrayecto')) AS [TipoTrayecto],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.medioPrueba'), JSON_VALUE(c.[DenunciaJson], '$.datos.medioPrueba')) AS [MedioPrueba],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.detallePrueba'), JSON_VALUE(c.[DenunciaJson], '$.datos.detallePrueba')) AS [DetallePrueba],
TRY_CONVERT(int, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.duracionSintomas'), JSON_VALUE(c.[DenunciaJson], '$.datos.duracionSintomas')), '')) AS [DuracionSintomas],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.duracionSintomasUnidad'), JSON_VALUE(c.[DenunciaJson], '$.datos.duracionSintomasUnidad')) AS [DuracionSintomasUnidad],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.molestiasPrevias'), JSON_VALUE(c.[DenunciaJson], '$.datos.molestiasPrevias')) AS [MolestiasPrevias],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.parteCuerpo'), JSON_VALUE(c.[DenunciaJson], '$.datos.parteCuerpo')) AS [ParteCuerpo],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.actividadSintomas'), JSON_VALUE(c.[DenunciaJson], '$.datos.actividadSintomas')) AS [ActividadSintomas],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.puestoSintomas'), JSON_VALUE(c.[DenunciaJson], '$.datos.puestoSintomas')) AS [PuestoSintomas],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.companerosSintomas'), JSON_VALUE(c.[DenunciaJson], '$.datos.companerosSintomas')) AS [CompanerosSintomas],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.agentes'), JSON_VALUE(c.[DenunciaJson], '$.datos.agentes')) AS [Agentes],
TRY_CONVERT(int, NULLIF(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.tiempoExposicion'), JSON_VALUE(c.[DenunciaJson], '$.datos.tiempoExposicion')), '')) AS [TiempoExposicion],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.tiempoExposicionUnidad'), JSON_VALUE(c.[DenunciaJson], '$.datos.tiempoExposicionUnidad')) AS [TiempoExposicionUnidad],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.denuncianteNombre'), JSON_VALUE(c.[DenunciaJson], '$.datos.denuncianteNombre')) AS [DenuncianteNombre],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.denuncianteRun'), JSON_VALUE(c.[DenunciaJson], '$.datos.denuncianteRun')) AS [DenuncianteRun],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.denuncianteTelefono'), JSON_VALUE(c.[DenunciaJson], '$.datos.denuncianteTelefono')) AS [DenuncianteTelefono],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.denuncianteClasificacion'), JSON_VALUE(c.[DenunciaJson], '$.datos.denuncianteClasificacion')) AS [DenuncianteClasificacion],
COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.firmaNombre'), JSON_VALUE(c.[DenunciaJson], '$.datos.firmaNombre')) AS [FirmaNombre],
UPPER(REPLACE(REPLACE(LTRIM(RTRIM(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.empleadorRut'), JSON_VALUE(c.[DenunciaJson], '$.datos.empleadorRut')))), '.', ''), '-', '')) AS [RutCanonico],
UPPER(REPLACE(REPLACE(LTRIM(RTRIM(COALESCE(JSON_VALUE(c.[DenunciaJson], '$.Datos.trabajadorRun'), JSON_VALUE(c.[DenunciaJson], '$.datos.trabajadorRun')))), '.', ''), '-', '')) AS [RunCanonico]
INTO #DenunciasLegadas
FROM [Casos] c WHERE ISJSON(c.[DenunciaJson]) = 1 AND c.[TrabajadorId] IS NOT NULL;
WITH elegidos AS (
 SELECT *, ROW_NUMBER() OVER (PARTITION BY [RutCanonico] ORDER BY [CreadoEn] DESC, [Id] DESC) AS orden
 FROM #DenunciasLegadas WHERE LEN([RutCanonico]) BETWEEN 8 AND 9
)
INSERT INTO [Empleadores] ([Id], [Rut], [Nombre], [Direccion], [Comuna], [Telefono], [ActividadEconomica], [TrabajadoresHombres], [TrabajadorasMujeres], [Propiedad], [TipoEmpresa], [ActividadPrincipal])
SELECT NEWID(), [RutCanonico], [EmpleadorNombre], [EmpleadorDireccion], [EmpleadorComuna], [EmpleadorTelefono], [ActividadEconomica], [TrabajadoresHombres], [TrabajadorasMujeres], [Propiedad], [TipoEmpresa], [ActividadPrincipal] FROM elegidos x WHERE x.orden = 1 AND NOT EXISTS (SELECT 1 FROM [Empleadores] e WHERE e.[Rut] = x.[RutCanonico]);
UPDATE c SET [EmpleadorId] = e.[Id] FROM [Casos] c JOIN #DenunciasLegadas x ON x.[Id] = c.[Id] JOIN [Empleadores] e ON e.[Rut] = x.[RutCanonico];
INSERT INTO [DenunciasCasos] ([Id], [CasoId], [EmpleadorId], [TrabajadorId], [Formulario], [FechaEmision], [CodigoOrganismo], [Folio], [EmpleadorNombre], [EmpleadorRut], [EmpleadorDireccion], [EmpleadorComuna], [EmpleadorTelefono], [ActividadEconomica], [TrabajadoresHombres], [TrabajadorasMujeres], [Propiedad], [TipoEmpresa], [ActividadPrincipal], [TrabajadorNombre], [TrabajadorRun], [TrabajadorDireccion], [TrabajadorComuna], [TrabajadorTelefono], [Sexo], [FechaNacimiento], [Nacionalidad], [Profesion], [Antiguedad], [AntiguedadUnidad], [TipoContrato], [TipoIngreso], [CategoriaOcupacional], [PuebloOriginario], [OtroPueblo], [HoraAccidente], [HoraIngreso], [HoraSalida], [AccidenteDireccion], [AccidenteComuna], [ActividadAccidente], [LugarAccidente], [TrabajoHabitual], [RealizabaHabitual], [ClasificacionAccidente], [TipoAccidente], [TipoTrayecto], [MedioPrueba], [DetallePrueba], [DuracionSintomas], [DuracionSintomasUnidad], [MolestiasPrevias], [ParteCuerpo], [ActividadSintomas], [PuestoSintomas], [CompanerosSintomas], [Agentes], [TiempoExposicion], [TiempoExposicionUnidad], [DenuncianteNombre], [DenuncianteRun], [DenuncianteTelefono], [DenuncianteClasificacion], [FirmaNombre])
SELECT NEWID(), x.[Id], e.[Id], x.[TrabajadorId], x.[Formulario], x.[FechaEmision], x.[CodigoOrganismo], x.[Folio], x.[EmpleadorNombre], x.[EmpleadorRut], x.[EmpleadorDireccion], x.[EmpleadorComuna], x.[EmpleadorTelefono], x.[ActividadEconomica], x.[TrabajadoresHombres], x.[TrabajadorasMujeres], x.[Propiedad], x.[TipoEmpresa], x.[ActividadPrincipal], x.[TrabajadorNombre], x.[TrabajadorRun], x.[TrabajadorDireccion], x.[TrabajadorComuna], x.[TrabajadorTelefono], x.[Sexo], x.[FechaNacimiento], x.[Nacionalidad], x.[Profesion], x.[Antiguedad], x.[AntiguedadUnidad], x.[TipoContrato], x.[TipoIngreso], x.[CategoriaOcupacional], x.[PuebloOriginario], x.[OtroPueblo], x.[HoraAccidente], x.[HoraIngreso], x.[HoraSalida], x.[AccidenteDireccion], x.[AccidenteComuna], x.[ActividadAccidente], x.[LugarAccidente], x.[TrabajoHabitual], x.[RealizabaHabitual], x.[ClasificacionAccidente], x.[TipoAccidente], x.[TipoTrayecto], x.[MedioPrueba], x.[DetallePrueba], x.[DuracionSintomas], x.[DuracionSintomasUnidad], x.[MolestiasPrevias], x.[ParteCuerpo], x.[ActividadSintomas], x.[PuestoSintomas], x.[CompanerosSintomas], x.[Agentes], x.[TiempoExposicion], x.[TiempoExposicionUnidad], x.[DenuncianteNombre], x.[DenuncianteRun], x.[DenuncianteTelefono], x.[DenuncianteClasificacion], x.[FirmaNombre]
FROM #DenunciasLegadas x JOIN [Empleadores] e ON e.[Rut] = x.[RutCanonico] WHERE NOT EXISTS (SELECT 1 FROM [DenunciasCasos] d WHERE d.[CasoId] = x.[Id]);
WITH ultimos AS (
 SELECT *, ROW_NUMBER() OVER (PARTITION BY [TrabajadorId] ORDER BY [CreadoEn] DESC, [Id] DESC) AS orden FROM #DenunciasLegadas
), duenos AS (SELECT [RunCanonico], COUNT(DISTINCT [TrabajadorId]) AS cantidad FROM #DenunciasLegadas GROUP BY [RunCanonico])
UPDATE t SET [Run] = CASE WHEN duenos.cantidad = 1 AND LEN(x.[RunCanonico]) BETWEEN 8 AND 9 THEN x.[RunCanonico] ELSE NULL END,
[Nombre] = COALESCE(NULLIF(LEFT(x.[TrabajadorNombre],120), ''), t.[Nombre]),
[Cargo] = COALESCE(NULLIF(LEFT(x.[Profesion],100), ''), t.[Cargo]),
t.[Direccion] = x.[TrabajadorDireccion],
t.[Comuna] = x.[TrabajadorComuna],
t.[Telefono] = x.[TrabajadorTelefono],
t.[Sexo] = x.[Sexo],
t.[FechaNacimiento] = x.[FechaNacimiento],
t.[Nacionalidad] = x.[Nacionalidad],
t.[PuebloOriginario] = x.[PuebloOriginario],
t.[OtroPueblo] = x.[OtroPueblo]
FROM [Trabajadores] t JOIN ultimos x ON x.[TrabajadorId] = t.[Id] AND x.orden = 1 LEFT JOIN duenos ON duenos.[RunCanonico] = x.[RunCanonico];
IF EXISTS (SELECT 1 FROM #DenunciasLegadas x WHERE NOT EXISTS (SELECT 1 FROM [DenunciasCasos] d WHERE d.[CasoId] = x.[Id]))
 THROW 51001, 'No se pudieron trasladar todas las denuncias. La migración se cancela para conservar los datos.', 1;
DROP TABLE #DenunciasLegadas;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Casos_Empleadores_EmpleadorId",
                table: "Casos");

            migrationBuilder.DropTable(
                name: "DenunciasCasos");

            migrationBuilder.DropTable(
                name: "Empleadores");

            migrationBuilder.DropIndex(
                name: "IX_Trabajadores_Run",
                table: "Trabajadores");

            migrationBuilder.DropIndex(
                name: "IX_Casos_EmpleadorId",
                table: "Casos");

            migrationBuilder.DropColumn(
                name: "Comuna",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "Nacionalidad",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "OtroPueblo",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "PuebloOriginario",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "Run",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "Sexo",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Trabajadores");

            migrationBuilder.DropColumn(
                name: "EmpleadorId",
                table: "Casos");
        }
    }
}
