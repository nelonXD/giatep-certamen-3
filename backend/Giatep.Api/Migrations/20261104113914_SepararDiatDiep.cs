using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giatep.Api.Migrations
{
    /// <inheritdoc />
    public partial class SepararDiatDiep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
migrationBuilder.DropTable(
                name: "DenunciasCasos");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
migrationBuilder.CreateTable(
                name: "DenunciasCasos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccidenteComuna = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AccidenteDireccion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadEconomica = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadPrincipal = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Agentes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Antiguedad = table.Column<int>(type: "int", nullable: true),
                    AntiguedadUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CasoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaOcupacional = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ClasificacionAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CodigoOrganismo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanerosSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteClasificacion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteRun = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DenuncianteTelefono = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DetallePrueba = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DuracionSintomas = table.Column<int>(type: "int", nullable: true),
                    DuracionSintomasUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorComuna = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorDireccion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadorNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorRut = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EmpleadorTelefono = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaIngreso = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    FirmaNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Folio = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Formulario = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    HoraAccidente = table.Column<TimeOnly>(type: "time", nullable: true),
                    HoraIngreso = table.Column<TimeOnly>(type: "time", nullable: true),
                    HoraSalida = table.Column<TimeOnly>(type: "time", nullable: true),
                    LugarAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MedioPrueba = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MolestiasPrevias = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Nacionalidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OtroPueblo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ParteCuerpo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Profesion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Propiedad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PuebloOriginario = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PuestoSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RealizabaHabitual = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Sexo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TiempoExposicion = table.Column<int>(type: "int", nullable: true),
                    TiempoExposicionUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoAccidente = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoContrato = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoEmpresa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoIngreso = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TipoTrayecto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorComuna = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorDireccion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrabajadorNombre = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorRun = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorTelefono = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TrabajadorasMujeres = table.Column<int>(type: "int", nullable: true),
                    TrabajadoresHombres = table.Column<int>(type: "int", nullable: true),
                    TrabajoHabitual = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
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
migrationBuilder.Sql("""
INSERT INTO [DenunciasCasos] ([Id], [CasoId], [EmpleadorId], [TrabajadorId], [Formulario], [FechaEmision], [CodigoOrganismo], [Folio], [EmpleadorNombre], [EmpleadorRut], [EmpleadorDireccion], [EmpleadorComuna], [EmpleadorTelefono], [ActividadEconomica], [TrabajadoresHombres], [TrabajadorasMujeres], [Propiedad], [TipoEmpresa], [ActividadPrincipal], [TrabajadorNombre], [TrabajadorRun], [TrabajadorDireccion], [TrabajadorComuna], [TrabajadorTelefono], [Sexo], [FechaNacimiento], [Nacionalidad], [Profesion], [Antiguedad], [FechaIngreso], [AntiguedadUnidad], [TipoContrato], [TipoIngreso], [CategoriaOcupacional], [PuebloOriginario], [OtroPueblo], [HoraAccidente], [HoraIngreso], [HoraSalida], [AccidenteDireccion], [AccidenteComuna], [ActividadAccidente], [LugarAccidente], [TrabajoHabitual], [RealizabaHabitual], [ClasificacionAccidente], [TipoAccidente], [TipoTrayecto], [MedioPrueba], [DetallePrueba], [DuracionSintomas], [DuracionSintomasUnidad], [MolestiasPrevias], [ParteCuerpo], [ActividadSintomas], [PuestoSintomas], [CompanerosSintomas], [Agentes], [TiempoExposicion], [TiempoExposicionUnidad], [DenuncianteNombre], [DenuncianteRun], [DenuncianteTelefono], [DenuncianteClasificacion], [FirmaNombre]) SELECT j.[Id], j.[CasoId], j.[EmpleadorId], j.[TrabajadorId], j.[Formulario], j.[FechaEmision], j.[CodigoOrganismo], j.[Folio], j.[EmpleadorNombre], j.[EmpleadorRut], j.[EmpleadorDireccion], j.[EmpleadorComuna], j.[EmpleadorTelefono], j.[ActividadEconomica], j.[TrabajadoresHombres], j.[TrabajadorasMujeres], j.[Propiedad], j.[TipoEmpresa], j.[ActividadPrincipal], j.[TrabajadorNombre], j.[TrabajadorRun], j.[TrabajadorDireccion], j.[TrabajadorComuna], j.[TrabajadorTelefono], j.[Sexo], j.[FechaNacimiento], j.[Nacionalidad], j.[Profesion], j.[Antiguedad], j.[FechaIngreso], j.[AntiguedadUnidad], j.[TipoContrato], j.[TipoIngreso], j.[CategoriaOcupacional], j.[PuebloOriginario], j.[OtroPueblo], j.[HoraAccidente], j.[HoraIngreso], j.[HoraSalida], j.[AccidenteDireccion], j.[AccidenteComuna], j.[ActividadAccidente], j.[LugarAccidente], j.[TrabajoHabitual], j.[RealizabaHabitual], j.[ClasificacionAccidente], j.[TipoAccidente], j.[TipoTrayecto], j.[MedioPrueba], j.[DetallePrueba], j.[DuracionSintomas], j.[DuracionSintomasUnidad], j.[MolestiasPrevias], j.[ParteCuerpo], j.[ActividadSintomas], j.[PuestoSintomas], j.[CompanerosSintomas], j.[Agentes], j.[TiempoExposicion], j.[TiempoExposicionUnidad], j.[DenuncianteNombre], j.[DenuncianteRun], j.[DenuncianteTelefono], j.[DenuncianteClasificacion], j.[FirmaNombre] FROM [Auditorias] a CROSS APPLY OPENJSON(a.[Cambios]) WITH ([Id] uniqueidentifier '$.Id', [CasoId] uniqueidentifier '$.CasoId', [EmpleadorId] uniqueidentifier '$.EmpleadorId', [TrabajadorId] uniqueidentifier '$.TrabajadorId', [Formulario] nvarchar(2000) '$.Formulario', [FechaEmision] date '$.FechaEmision', [CodigoOrganismo] nvarchar(2000) '$.CodigoOrganismo', [Folio] nvarchar(2000) '$.Folio', [EmpleadorNombre] nvarchar(2000) '$.EmpleadorNombre', [EmpleadorRut] nvarchar(2000) '$.EmpleadorRut', [EmpleadorDireccion] nvarchar(2000) '$.EmpleadorDireccion', [EmpleadorComuna] nvarchar(2000) '$.EmpleadorComuna', [EmpleadorTelefono] nvarchar(2000) '$.EmpleadorTelefono', [ActividadEconomica] nvarchar(2000) '$.ActividadEconomica', [TrabajadoresHombres] int '$.TrabajadoresHombres', [TrabajadorasMujeres] int '$.TrabajadorasMujeres', [Propiedad] nvarchar(2000) '$.Propiedad', [TipoEmpresa] nvarchar(2000) '$.TipoEmpresa', [ActividadPrincipal] nvarchar(2000) '$.ActividadPrincipal', [TrabajadorNombre] nvarchar(2000) '$.TrabajadorNombre', [TrabajadorRun] nvarchar(2000) '$.TrabajadorRun', [TrabajadorDireccion] nvarchar(2000) '$.TrabajadorDireccion', [TrabajadorComuna] nvarchar(2000) '$.TrabajadorComuna', [TrabajadorTelefono] nvarchar(2000) '$.TrabajadorTelefono', [Sexo] nvarchar(2000) '$.Sexo', [FechaNacimiento] date '$.FechaNacimiento', [Nacionalidad] nvarchar(2000) '$.Nacionalidad', [Profesion] nvarchar(2000) '$.Profesion', [Antiguedad] int '$.Antiguedad', [FechaIngreso] nvarchar(2000) '$.FechaIngreso', [AntiguedadUnidad] nvarchar(2000) '$.AntiguedadUnidad', [TipoContrato] nvarchar(2000) '$.TipoContrato', [TipoIngreso] nvarchar(2000) '$.TipoIngreso', [CategoriaOcupacional] nvarchar(2000) '$.CategoriaOcupacional', [PuebloOriginario] nvarchar(2000) '$.PuebloOriginario', [OtroPueblo] nvarchar(2000) '$.OtroPueblo', [HoraAccidente] time '$.HoraAccidente', [HoraIngreso] time '$.HoraIngreso', [HoraSalida] time '$.HoraSalida', [AccidenteDireccion] nvarchar(2000) '$.AccidenteDireccion', [AccidenteComuna] nvarchar(2000) '$.AccidenteComuna', [ActividadAccidente] nvarchar(2000) '$.ActividadAccidente', [LugarAccidente] nvarchar(2000) '$.LugarAccidente', [TrabajoHabitual] nvarchar(2000) '$.TrabajoHabitual', [RealizabaHabitual] nvarchar(2000) '$.RealizabaHabitual', [ClasificacionAccidente] nvarchar(2000) '$.ClasificacionAccidente', [TipoAccidente] nvarchar(2000) '$.TipoAccidente', [TipoTrayecto] nvarchar(2000) '$.TipoTrayecto', [MedioPrueba] nvarchar(2000) '$.MedioPrueba', [DetallePrueba] nvarchar(2000) '$.DetallePrueba', [DuracionSintomas] int '$.DuracionSintomas', [DuracionSintomasUnidad] nvarchar(2000) '$.DuracionSintomasUnidad', [MolestiasPrevias] nvarchar(2000) '$.MolestiasPrevias', [ParteCuerpo] nvarchar(2000) '$.ParteCuerpo', [ActividadSintomas] nvarchar(2000) '$.ActividadSintomas', [PuestoSintomas] nvarchar(2000) '$.PuestoSintomas', [CompanerosSintomas] nvarchar(2000) '$.CompanerosSintomas', [Agentes] nvarchar(2000) '$.Agentes', [TiempoExposicion] int '$.TiempoExposicion', [TiempoExposicionUnidad] nvarchar(2000) '$.TiempoExposicionUnidad', [DenuncianteNombre] nvarchar(2000) '$.DenuncianteNombre', [DenuncianteRun] nvarchar(2000) '$.DenuncianteRun', [DenuncianteTelefono] nvarchar(2000) '$.DenuncianteTelefono', [DenuncianteClasificacion] nvarchar(2000) '$.DenuncianteClasificacion', [FirmaNombre] nvarchar(2000) '$.FirmaNombre') j WHERE a.[Accion]=N'ArchivoNormalizacion';
""");
        }
    }
}
