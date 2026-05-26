using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapShotsLK.API.Migrations
{
    /// <inheritdoc />
    public partial class FixIsApprovedDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing Client, admin, and superadmin users should be approved
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"IsApproved\" = true WHERE \"Role\" IN ('Client', 'admin', 'superadmin')"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
