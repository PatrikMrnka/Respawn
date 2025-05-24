using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RespawnApi.Migrations
{
    /// <inheritdoc />
    public partial class containerStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LgsmServerStatus",
                table: "GameServers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LgsmServerStatus",
                table: "GameServers");
        }
    }
}
