using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UowMiCakeApplication.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Itinerarys",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Participants = table.Column<string>(nullable: true),
                    Places = table.Column<string>(nullable: true),
                    Note = table.Column<string>(nullable: true),
                    TripTime = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itinerarys", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Itinerarys");
        }
    }
}
