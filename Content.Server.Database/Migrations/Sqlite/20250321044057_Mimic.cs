using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class Mimic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mimic_phrase_prob",
                columns: table => new
                {
                    mimic_phrase_prob_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    prototype_id = table.Column<string>(type: "TEXT", nullable: false),
                    phrase = table.Column<string>(type: "TEXT", nullable: false),
                    prob = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mimic_phrase_prob", x => x.mimic_phrase_prob_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mimic_phrase_prob");
        }
    }
}
