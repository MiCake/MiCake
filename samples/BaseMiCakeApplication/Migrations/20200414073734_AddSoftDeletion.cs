using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace BaseMiCakeApplication.Migrations
{
    public partial class AddSoftDeletion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "Itinerary",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Itinerary",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificationTime",
                table: "Itinerary",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "Itinerary");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Itinerary");

            migrationBuilder.DropColumn(
                name: "ModificationTime",
                table: "Itinerary");
        }
    }
}
