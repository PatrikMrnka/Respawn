using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RespawnApi.Migrations
{
    /// <inheritdoc />
    public partial class gameServers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_GameServers_ServerId",
                table: "GameSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStats_GameServers_ServerId",
                table: "PlayerStats");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStats_GameSessions_GameSessionId",
                table: "PlayerStats");

            migrationBuilder.DropForeignKey(
                name: "FK_Polls_UserProfiles_CreatorUserId",
                table: "Polls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameSessions",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_ServerId",
                table: "GameSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameServers",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "Configuration",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "DockerImage",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "Metrics",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "PortMappings",
                table: "GameServers");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServerId",
                table: "PlayerStats",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<Guid>(
                name: "GameSessionId",
                table: "PlayerStats",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldMaxLength: 36,
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<Guid>(
                name: "StatsId",
                table: "PlayerStats",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldMaxLength: 36)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "GameSessions",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AddColumn<Guid>(
                name: "GameSessionId",
                table: "GameSessions",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPlayers",
                table: "GameSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "GameServerId",
                table: "GameSessions",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "MapName",
                table: "GameSessions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MaxPlayers",
                table: "GameSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "ContainerId",
                table: "GameServers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "GameServerId",
                table: "GameServers",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "GameServers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "GameServers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "GameServers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusDetails",
                table: "GameServers",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameSessions",
                table: "GameSessions",
                column: "GameSessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameServers",
                table: "GameServers",
                column: "GameServerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_GameServerId",
                table: "GameSessions",
                column: "GameServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_GameServers_GameServerId",
                table: "GameSessions",
                column: "GameServerId",
                principalTable: "GameServers",
                principalColumn: "GameServerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStats_GameServers_ServerId",
                table: "PlayerStats",
                column: "ServerId",
                principalTable: "GameServers",
                principalColumn: "GameServerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStats_GameSessions_GameSessionId",
                table: "PlayerStats",
                column: "GameSessionId",
                principalTable: "GameSessions",
                principalColumn: "GameSessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Polls_UserProfiles_CreatorUserId",
                table: "Polls",
                column: "CreatorUserId",
                principalTable: "UserProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameSessions_GameServers_GameServerId",
                table: "GameSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStats_GameServers_ServerId",
                table: "PlayerStats");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStats_GameSessions_GameSessionId",
                table: "PlayerStats");

            migrationBuilder.DropForeignKey(
                name: "FK_Polls_UserProfiles_CreatorUserId",
                table: "Polls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameSessions",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_GameServerId",
                table: "GameSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GameServers",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "GameSessionId",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "CurrentPlayers",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "GameServerId",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "MapName",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "MaxPlayers",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "GameServerId",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "GameServers");

            migrationBuilder.DropColumn(
                name: "StatusDetails",
                table: "GameServers");

            migrationBuilder.AlterColumn<string>(
                name: "ServerId",
                table: "PlayerStats",
                type: "varchar(36)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "GameSessionId",
                table: "PlayerStats",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "StatsId",
                table: "PlayerStats",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "GameSessions",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "GameSessions",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ServerId",
                table: "GameSessions",
                type: "varchar(36)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ContainerId",
                table: "GameServers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ServerId",
                table: "GameServers",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Configuration",
                table: "GameServers",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DockerImage",
                table: "GameServers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Metrics",
                table: "GameServers",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PortMappings",
                table: "GameServers",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameSessions",
                table: "GameSessions",
                column: "SessionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GameServers",
                table: "GameServers",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_ServerId",
                table: "GameSessions",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSessions_GameServers_ServerId",
                table: "GameSessions",
                column: "ServerId",
                principalTable: "GameServers",
                principalColumn: "ServerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStats_GameServers_ServerId",
                table: "PlayerStats",
                column: "ServerId",
                principalTable: "GameServers",
                principalColumn: "ServerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStats_GameSessions_GameSessionId",
                table: "PlayerStats",
                column: "GameSessionId",
                principalTable: "GameSessions",
                principalColumn: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Polls_UserProfiles_CreatorUserId",
                table: "Polls",
                column: "CreatorUserId",
                principalTable: "UserProfiles",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
