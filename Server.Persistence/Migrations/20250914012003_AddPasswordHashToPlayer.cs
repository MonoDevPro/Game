﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordHashToPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "PlayerModels",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "PlayerModels");
        }
    }
}
