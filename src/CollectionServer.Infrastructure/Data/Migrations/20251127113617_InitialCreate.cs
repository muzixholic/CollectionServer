using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CollectionServer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Isbn13 = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    Authors = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Publisher = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublishDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    Genre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Director = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Cast = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RuntimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Rating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Movie_Genre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Artist = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MusicAlbum_ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MusicAlbum_Genre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Tracks = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_barcode",
                table: "MediaItems",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_media_type",
                table: "MediaItems",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "idx_title",
                table: "MediaItems",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaItems");
        }
    }
}
