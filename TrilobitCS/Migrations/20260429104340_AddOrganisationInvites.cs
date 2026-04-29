using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrilobitCS.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganisationInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organisations_InviteCode",
                table: "Organisations");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Organisations");

            migrationBuilder.CreateTable(
                name: "OrganisationInvites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganisationId = table.Column<int>(type: "integer", nullable: false),
                    InvitedUserId = table.Column<int>(type: "integer", nullable: false),
                    InvitedById = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganisationInvites_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganisationInvites_Users_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrganisationInvites_Users_InvitedUserId",
                        column: x => x.InvitedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationInvites_InvitedById",
                table: "OrganisationInvites",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationInvites_InvitedUserId_OrganisationId",
                table: "OrganisationInvites",
                columns: new[] { "InvitedUserId", "OrganisationId" },
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationInvites_OrganisationId",
                table: "OrganisationInvites",
                column: "OrganisationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganisationInvites");

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Organisations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_InviteCode",
                table: "Organisations",
                column: "InviteCode",
                unique: true);
        }
    }
}
