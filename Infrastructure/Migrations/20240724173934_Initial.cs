using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Recurring = table.Column<bool>(type: "bit", nullable: false),
                    RenewOnDueDate = table.Column<bool>(type: "bit", nullable: true),
                    IntervalValue = table.Column<int>(type: "int", nullable: true),
                    IntervalType = table.Column<int>(type: "int", nullable: true),
                    RowIndex = table.Column<int>(type: "int", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notepads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notepads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowIndex = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemTasks_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_CommittedDate",
                table: "ItemTasks",
                column: "CommittedDate");

            migrationBuilder.CreateIndex(
                name: "IDX_CompletionDate",
                table: "ItemTasks",
                column: "CompletionDate",
                filter: "CompletionDate IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IDX_DueDate",
                table: "ItemTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IDX_ItemID",
                table: "ItemTasks",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemTasks");

            migrationBuilder.DropTable(
                name: "Notepads");

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}
