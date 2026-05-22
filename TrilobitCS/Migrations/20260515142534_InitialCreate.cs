using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TrilobitCS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EagleFeathers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Light = table.Column<byte>(type: "smallint", nullable: false),
                    Section = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Challenge = table.Column<string>(type: "text", nullable: false),
                    GrandChallenge = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EagleFeathers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EagleFeatherId = table.Column<int>(type: "integer", nullable: false),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_EagleFeathers_EagleFeatherId",
                        column: x => x.EagleFeatherId,
                        principalTable: "EagleFeathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CommentableType = table.Column<int>(type: "integer", nullable: false),
                    CommentableId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Followers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FollowerId = table.Column<int>(type: "integer", nullable: false),
                    FollowingId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Followers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LikeableType = table.Column<int>(type: "integer", nullable: false),
                    LikeableId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => x.Id);
                });

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
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeclinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationInvites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    LeaderId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nickname = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ProfilePicture = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    OrganisationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    PinnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UnpinnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChallenges_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEagleFeathers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EagleFeatherId = table.Column<int>(type: "integer", nullable: false),
                    IsGrandChallenge = table.Column<bool>(type: "boolean", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VerifiedById = table.Column<int>(type: "integer", nullable: true),
                    EarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEagleFeathers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEagleFeathers_EagleFeathers_EagleFeatherId",
                        column: x => x.EagleFeatherId,
                        principalTable: "EagleFeathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEagleFeathers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEagleFeathers_Users_VerifiedById",
                        column: x => x.VerifiedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OrganisationId = table.Column<int>(type: "integer", nullable: true),
                    UserEagleFeatherId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Posts_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Posts_UserEagleFeathers_UserEagleFeatherId",
                        column: x => x.UserEagleFeatherId,
                        principalTable: "UserEagleFeathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Posts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_EagleFeatherId",
                table: "Challenges",
                column: "EagleFeatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CommentableType_CommentableId",
                table: "Comments",
                columns: new[] { "CommentableType", "CommentableId" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EagleFeathers_Light_Section_Number",
                table: "EagleFeathers",
                columns: new[] { "Light", "Section", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowerId_FollowingId",
                table: "Followers",
                columns: new[] { "FollowerId", "FollowingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Followers_FollowingId",
                table: "Followers",
                column: "FollowingId");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_LikeableType_LikeableId",
                table: "Likes",
                columns: new[] { "LikeableType", "LikeableId" });

            migrationBuilder.CreateIndex(
                name: "IX_Likes_UserId_LikeableType_LikeableId",
                table: "Likes",
                columns: new[] { "UserId", "LikeableType", "LikeableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationInvites_InvitedById",
                table: "OrganisationInvites",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationInvites_InvitedUserId_OrganisationId_DeclinedAt",
                table: "OrganisationInvites",
                columns: new[] { "InvitedUserId", "OrganisationId", "DeclinedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationInvites_OrganisationId",
                table: "OrganisationInvites",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_LeaderId",
                table: "Organisations",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ChallengeId",
                table: "Posts",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_OrganisationId",
                table: "Posts",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserEagleFeatherId",
                table: "Posts",
                column: "UserEagleFeatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallenges_ChallengeId",
                table: "UserChallenges",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChallenges_UserId_ChallengeId",
                table: "UserChallenges",
                columns: new[] { "UserId", "ChallengeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEagleFeathers_EagleFeatherId",
                table: "UserEagleFeathers",
                column: "EagleFeatherId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEagleFeathers_UserId_EagleFeatherId",
                table: "UserEagleFeathers",
                columns: new[] { "UserId", "EagleFeatherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEagleFeathers_VerifiedById",
                table: "UserEagleFeathers",
                column: "VerifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Nickname",
                table: "Users",
                column: "Nickname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganisationId",
                table: "Users",
                column: "OrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Users_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Followers_Users_FollowerId",
                table: "Followers",
                column: "FollowerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Followers_Users_FollowingId",
                table: "Followers",
                column: "FollowingId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Likes_Users_UserId",
                table: "Likes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationInvites_Organisations_OrganisationId",
                table: "OrganisationInvites",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationInvites_Users_InvitedById",
                table: "OrganisationInvites",
                column: "InvitedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationInvites_Users_InvitedUserId",
                table: "OrganisationInvites",
                column: "InvitedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organisations_Users_LeaderId",
                table: "Organisations",
                column: "LeaderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("ALTER TABLE \"Followers\" ADD CONSTRAINT \"ck_followers_no_self_follow\" CHECK (\"FollowerId\" <> \"FollowingId\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Followers\" DROP CONSTRAINT IF EXISTS \"ck_followers_no_self_follow\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Organisations_Users_LeaderId",
                table: "Organisations");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Followers");

            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropTable(
                name: "OrganisationInvites");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserChallenges");

            migrationBuilder.DropTable(
                name: "UserEagleFeathers");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "EagleFeathers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Organisations");
        }
    }
}
