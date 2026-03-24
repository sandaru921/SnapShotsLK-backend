using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SnapShotsLK.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessionalProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfessionalProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Bio = table.Column<string>(type: "text", nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "text", nullable: false),
                    Experience = table.Column<string>(type: "text", nullable: false),
                    About = table.Column<string>(type: "text", nullable: false),
                    ResponseTime = table.Column<string>(type: "text", nullable: false),
                    Availability = table.Column<string>(type: "text", nullable: false),
                    InstagramUrl = table.Column<string>(type: "text", nullable: false),
                    FacebookUrl = table.Column<string>(type: "text", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: false),
                    Specialties = table.Column<List<string>>(type: "text[]", nullable: false),
                    PortfolioUrls = table.Column<List<string>>(type: "text[]", nullable: false),
                    Languages = table.Column<List<string>>(type: "text[]", nullable: false),
                    Achievements = table.Column<List<string>>(type: "text[]", nullable: false),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalProfiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_ProfessionalProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    ReviewerName = table.Column<string>(type: "text", nullable: false),
                    ReviewerAvatar = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Images = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_Reviews_ProfessionalProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    PackageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<string>(type: "text", nullable: false),
                    Duration = table.Column<string>(type: "text", nullable: false),
                    IsPopular = table.Column<bool>(type: "boolean", nullable: false),
                    Features = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.PackageId);
                    table.ForeignKey(
                        name: "FK_ServicePackages_ProfessionalProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalProfiles_UserId",
                table: "ProfessionalProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProfileId",
                table: "Reviews",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_ProfileId",
                table: "ServicePackages",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "ServicePackages");

            migrationBuilder.DropTable(
                name: "ProfessionalProfiles");
        }
    }
}
