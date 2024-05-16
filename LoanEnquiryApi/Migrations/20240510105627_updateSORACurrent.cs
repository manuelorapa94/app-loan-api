using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanEnquiryApi.Migrations
{
    /// <inheritdoc />
    public partial class updateSORACurrent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SoraRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SoraRate1M = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SoraRate3M = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SoraRate6M = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoraRates", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoraRates");
        }
    }
}
