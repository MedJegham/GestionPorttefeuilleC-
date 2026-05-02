using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionPortefeuille.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioValuePoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioValuePoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalValue = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioValuePoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioValuePoints_Date",
                table: "PortfolioValuePoints",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioValuePoints");
        }
    }
}
