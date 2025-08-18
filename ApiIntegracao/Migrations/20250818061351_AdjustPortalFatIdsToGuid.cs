using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiIntegracao.Migrations
{
    /// <inheritdoc />
    public partial class AdjustPortalFatIdsToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "IdPortalFat",
                table: "Turmas",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "char(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<Guid>(
                name: "DisciplinaIdPortalFat",
                table: "Turmas",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "char(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<Guid>(
                name: "IdPortalFat",
                table: "Cursos",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(string),
                oldType: "char(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IdPortalFat",
                table: "Turmas",
                type: "char(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "DisciplinaIdPortalFat",
                table: "Turmas",
                type: "char(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "IdPortalFat",
                table: "Cursos",
                type: "char(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");
        }
    }
}
