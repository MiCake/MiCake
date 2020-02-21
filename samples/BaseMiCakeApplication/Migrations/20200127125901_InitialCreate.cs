using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace BaseMiCakeApplication.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Itinerary",
                columns: table => new
                {
                    ID = table.Column<Guid>(nullable: false),
                    Content = table.Column<string>(nullable: true),
                    NoteTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itinerary", x => x.ID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Itinerary");
        }
    }
}
