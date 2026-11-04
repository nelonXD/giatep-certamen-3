using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giatep.Api.Migrations
{
    /// <inheritdoc />
    public partial class InvestigacionSeparadaYPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Medidas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PropuestaId",
                table: "Medidas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InvestigacionId",
                table: "Evidencias",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Investigaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CasoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Antecedentes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatoFinal = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    IniciadaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActualizadaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CerradaEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PlanIniciadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EsHistorica = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investigaciones", x => x.Id);
                    table.CheckConstraint("CK_Investigaciones_Estado", "[Estado] IN ('EnCurso','Cerrada','Historica')");
                    table.ForeignKey(
                        name: "FK_Investigaciones_Casos_CasoId",
                        column: x => x.CasoId,
                        principalTable: "Casos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Entrevistas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestigacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Entrevistado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Calidad = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Declaracion = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrevistas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entrevistas_Investigaciones_InvestigacionId",
                        column: x => x.InvestigacionId,
                        principalTable: "Investigaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Hechos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestigacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EsFinal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hechos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hechos_Investigaciones_InvestigacionId",
                        column: x => x.InvestigacionId,
                        principalTable: "Investigaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedidasInvestigacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestigacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HechoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UsuarioResponsableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Plazo = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedidasInvestigacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedidasInvestigacion_Hechos_HechoId",
                        column: x => x.HechoId,
                        principalTable: "Hechos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedidasInvestigacion_Investigaciones_InvestigacionId",
                        column: x => x.InvestigacionId,
                        principalTable: "Investigaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedidasInvestigacion_Usuarios_UsuarioResponsableId",
                        column: x => x.UsuarioResponsableId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RelacionesCausales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestigacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AntecedenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsecuenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelacionesCausales", x => x.Id);
                    table.CheckConstraint("CK_Relaciones_NoAutoRelacion", "[AntecedenteId] <> [ConsecuenciaId]");
                    table.ForeignKey(
                        name: "FK_RelacionesCausales_Hechos_AntecedenteId",
                        column: x => x.AntecedenteId,
                        principalTable: "Hechos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelacionesCausales_Hechos_ConsecuenciaId",
                        column: x => x.ConsecuenciaId,
                        principalTable: "Hechos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RelacionesCausales_Investigaciones_InvestigacionId",
                        column: x => x.InvestigacionId,
                        principalTable: "Investigaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medidas_PropuestaId",
                table: "Medidas",
                column: "PropuestaId",
                unique: true,
                filter: "[PropuestaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_InvestigacionId",
                table: "Evidencias",
                column: "InvestigacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Entrevistas_InvestigacionId",
                table: "Entrevistas",
                column: "InvestigacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Hechos_InvestigacionId",
                table: "Hechos",
                column: "InvestigacionId",
                unique: true,
                filter: "[EsFinal] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Investigaciones_CasoId",
                table: "Investigaciones",
                column: "CasoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedidasInvestigacion_HechoId",
                table: "MedidasInvestigacion",
                column: "HechoId");

            migrationBuilder.CreateIndex(
                name: "IX_MedidasInvestigacion_InvestigacionId",
                table: "MedidasInvestigacion",
                column: "InvestigacionId");

            migrationBuilder.CreateIndex(
                name: "IX_MedidasInvestigacion_UsuarioResponsableId",
                table: "MedidasInvestigacion",
                column: "UsuarioResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesCausales_AntecedenteId_ConsecuenciaId",
                table: "RelacionesCausales",
                columns: new[] { "AntecedenteId", "ConsecuenciaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesCausales_ConsecuenciaId",
                table: "RelacionesCausales",
                column: "ConsecuenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesCausales_InvestigacionId",
                table: "RelacionesCausales",
                column: "InvestigacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Evidencias_Investigaciones_InvestigacionId",
                table: "Evidencias",
                column: "InvestigacionId",
                principalTable: "Investigaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Medidas_MedidasInvestigacion_PropuestaId",
                table: "Medidas",
                column: "PropuestaId",
                principalTable: "MedidasInvestigacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("ALTER VIEW dbo.v_CasosOperativos AS SELECT c.Id, c.Codigo, c.Fecha, c.Tipo, c.Estado, c.EstablecimientoId, c.Establecimiento, c.TrabajadorId, c.CerradoEn, COUNT(m.Id) AS TotalMedidas FROM dbo.Casos c LEFT JOIN dbo.Medidas m ON m.CasoId=c.Id AND m.Activa=1 GROUP BY c.Id,c.Codigo,c.Fecha,c.Tipo,c.Estado,c.EstablecimientoId,c.Establecimiento,c.TrabajadorId,c.CerradoEn");
            migrationBuilder.Sql("ALTER VIEW dbo.v_SeguimientoMedidas AS SELECT m.Id, m.CasoId, c.Codigo, c.Fecha AS FechaCaso, c.EstablecimientoId, c.Establecimiento, m.Descripcion, m.Responsable, m.UsuarioResponsableId, m.Plazo, m.Completada FROM dbo.Medidas m INNER JOIN dbo.Casos c ON m.CasoId=c.Id WHERE m.Activa=1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER VIEW dbo.v_CasosOperativos AS SELECT c.Id, c.Codigo, c.Fecha, c.Tipo, c.Estado, c.EstablecimientoId, c.Establecimiento, c.TrabajadorId, c.CerradoEn, COUNT(m.Id) AS TotalMedidas FROM dbo.Casos c LEFT JOIN dbo.Medidas m ON m.CasoId=c.Id GROUP BY c.Id,c.Codigo,c.Fecha,c.Tipo,c.Estado,c.EstablecimientoId,c.Establecimiento,c.TrabajadorId,c.CerradoEn");
            migrationBuilder.Sql("ALTER VIEW dbo.v_SeguimientoMedidas AS SELECT m.Id, m.CasoId, c.Codigo, c.Fecha AS FechaCaso, c.EstablecimientoId, c.Establecimiento, m.Descripcion, m.Responsable, m.UsuarioResponsableId, m.Plazo, m.Completada FROM dbo.Medidas m INNER JOIN dbo.Casos c ON m.CasoId=c.Id");
            migrationBuilder.DropForeignKey(
                name: "FK_Evidencias_Investigaciones_InvestigacionId",
                table: "Evidencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Medidas_MedidasInvestigacion_PropuestaId",
                table: "Medidas");

            migrationBuilder.DropTable(
                name: "Entrevistas");

            migrationBuilder.DropTable(
                name: "MedidasInvestigacion");

            migrationBuilder.DropTable(
                name: "RelacionesCausales");

            migrationBuilder.DropTable(
                name: "Hechos");

            migrationBuilder.DropTable(
                name: "Investigaciones");

            migrationBuilder.DropIndex(
                name: "IX_Medidas_PropuestaId",
                table: "Medidas");

            migrationBuilder.DropIndex(
                name: "IX_Evidencias_InvestigacionId",
                table: "Evidencias");

            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Medidas");

            migrationBuilder.DropColumn(
                name: "PropuestaId",
                table: "Medidas");

            migrationBuilder.DropColumn(
                name: "InvestigacionId",
                table: "Evidencias");
        }
    }
}
