using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class ConsentRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            try
            {
                migrationBuilder.AddColumn<string>(
                    name: "consent_text",
                    table: "profile",
                    type: "TEXT",
                    nullable: false,
                    defaultValue: "");
            }catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "consent_text",
                table: "profile");
        }
    }
}
