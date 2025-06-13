using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite;

/// <inheritdoc />
public partial class EvilBarks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bark_proto",
            table: "profile",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "bark_pitch",
            table: "profile",
            type: "REAL",
            nullable: false,
            defaultValue: 1.0f);

        migrationBuilder.AddColumn<string>(
            name: "low_bark_var",
            table: "profile",
            type: "REAL",
            nullable: false,
            defaultValue: 0.1f);

        migrationBuilder.AddColumn<string>(
            name: "high_bark_var",
            table: "profile",
            type: "REAL",
            nullable: false,
            defaultValue: 0.5f);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bark_proto",
            table: "profile");

        migrationBuilder.DropColumn(
            name: "bark_pitch",
            table: "profile");

        migrationBuilder.DropColumn(
            name: "low_bark_var",
            table: "profile");

        migrationBuilder.DropColumn(
            name: "high_bark_var",
            table: "profile");
    }
}
