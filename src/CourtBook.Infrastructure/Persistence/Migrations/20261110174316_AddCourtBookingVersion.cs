using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtBook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourtBookingVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingVersion",
                table: "Courts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingVersion",
                table: "Courts");
        }
    }
}
