using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Giatep.Api.Models;
namespace Giatep.Api.Migrations;
[DbContext(typeof(GiatepDbContext))]
[Migration("20261104113912_CrearTablasDiatDiep")]
public class CrearTablasDiatDiep : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
migrationBuilder.AddColumn<int>(
                name: "Antiguedad",
                table: "Trabajadores",
                type: "int",
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "AntiguedadUnidad",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "CategoriaOcupacional",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "FechaIngreso",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "TipoIngreso",
                table: "Trabajadores",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "CodigoOrganismo",
                table: "Casos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "DenuncianteClasificacion",
                table: "Casos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "DenuncianteNombre",
                table: "Casos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "DenuncianteRun",
                table: "Casos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "DenuncianteTelefono",
                table: "Casos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<DateOnly>(
                name: "FechaEmision",
                table: "Casos",
                type: "date",
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "FirmaNombre",
                table: "Casos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.AddColumn<string>(
                name: "Folio",
                table: "Casos",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
migrationBuilder.CreateTable(
                name: "DIAT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CasoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    DetallePrueba = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DIAT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DIAT_Casos_CasoId",
                        column: x => x.CasoId,
                        principalTable: "Casos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
migrationBuilder.CreateTable(
                name: "DIEP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CasoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DuracionSintomas = table.Column<int>(type: "int", nullable: true),
                    DuracionSintomasUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MolestiasPrevias = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ParteCuerpo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActividadSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PuestoSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompanerosSintomas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Agentes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TiempoExposicion = table.Column<int>(type: "int", nullable: true),
                    TiempoExposicionUnidad = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DIEP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DIEP_Casos_CasoId",
                        column: x => x.CasoId,
                        principalTable: "Casos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
migrationBuilder.CreateIndex(
                name: "IX_DIAT_CasoId",
                table: "DIAT",
                column: "CasoId",
                unique: true);
migrationBuilder.CreateIndex(
                name: "IX_DIEP_CasoId",
                table: "DIEP",
                column: "CasoId",
                unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
migrationBuilder.DropTable(
                name: "DIAT");
migrationBuilder.DropTable(
                name: "DIEP");
migrationBuilder.DropColumn(
                name: "Antiguedad",
                table: "Trabajadores");
migrationBuilder.DropColumn(
                name: "AntiguedadUnidad",
                table: "Trabajadores");
migrationBuilder.DropColumn(
                name: "CategoriaOcupacional",
                table: "Trabajadores");
migrationBuilder.DropColumn(
                name: "FechaIngreso",
                table: "Trabajadores");
migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Trabajadores");
migrationBuilder.DropColumn(
                name: "TipoIngreso",
                table: "Trabajadores");
migrationBuilder.DropColumn(
                name: "CodigoOrganismo",
                table: "Casos");
migrationBuilder.DropColumn(
                name: "DenuncianteClasificacion",
                table: "Casos");
migrationBuilder.DropColumn(
                name: "DenuncianteNombre",
                table: "Casos");
migrationBuilder.DropColumn(
                name: "DenuncianteRun",
                table: "Casos");
migrationBuilder.DropColumn(
                name: "DenuncianteTelefono",
                table: "Casos");
migrationBuilder.DropColumn(
                name: "FechaEmision",
                table: "Casos");
migrationBuilder.DropColumn(
                name: "FirmaNombre",
                table: "Casos");
migrationBuilder.DropColumn(
                name: "Folio",
                table: "Casos");
    }
}
