using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giatep.Api.Migrations
{
    /// <inheritdoc />
    public partial class QuitarCantidadTrabajadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrabajadorasMujeres",
                table: "Empleadores");

            migrationBuilder.DropColumn(
                name: "TrabajadoresHombres",
                table: "Empleadores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrabajadorasMujeres",
                table: "Empleadores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrabajadoresHombres",
                table: "Empleadores",
                type: "int",
                nullable: true);
        }
    }
}
