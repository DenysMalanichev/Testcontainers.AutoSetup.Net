using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Testcontainers.AutoSetup.Tests.IntegrationTests.Migrations.EfMigrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShipToAddress_State",
                table: "Orders",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ItemOrdered_ProductName",
                table: "OrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ItemOrdered_PictureUri",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ItemOrdered_CatalogItemId",
                table: "OrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Catalog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "CatalogBrands",
                columns: new[] { "Id", "Brand" },
                values: new object[,]
                {
                    { 1, "Azure" },
                    { 2, ".NET" },
                    { 3, "Visual Studio" },
                    { 4, "SQL Server" },
                    { 5, "Other" }
                });

            migrationBuilder.InsertData(
                table: "CatalogTypes",
                columns: new[] { "Id", "Type" },
                values: new object[,]
                {
                    { 1, "Mug" },
                    { 2, "T-Shirt" },
                    { 3, "Sheet" },
                    { 4, "USB Memory Stick" }
                });

            migrationBuilder.InsertData(
                table: "Catalog",
                columns: new[] { "Id", "CatalogBrandId", "CatalogTypeId", "Description", "Name", "PictureUri", "Price" },
                values: new object[,]
                {
                    { 1, 2, 2, ".NET Bot Black Sweatshirt", ".NET Bot Black Sweatshirt", "http://catalogbaseurltobereplaced/images/products/1.png", 19.5m },
                    { 2, 2, 1, ".NET Black & White Mug", ".NET Black & White Mug", "http://catalogbaseurltobereplaced/images/products/2.png", 8.50m },
                    { 3, 5, 2, "Prism White T-Shirt", "Prism White T-Shirt", "http://catalogbaseurltobereplaced/images/products/3.png", 12m },
                    { 4, 2, 2, ".NET Foundation Sweatshirt", ".NET Foundation Sweatshirt", "http://catalogbaseurltobereplaced/images/products/4.png", 12m },
                    { 5, 5, 3, "Roslyn Red Sheet", "Roslyn Red Sheet", "http://catalogbaseurltobereplaced/images/products/5.png", 8.5m },
                    { 6, 2, 2, ".NET Blue Sweatshirt", ".NET Blue Sweatshirt", "http://catalogbaseurltobereplaced/images/products/6.png", 12m },
                    { 7, 5, 2, "Roslyn Red T-Shirt", "Roslyn Red T-Shirt", "http://catalogbaseurltobereplaced/images/products/7.png", 12m },
                    { 8, 5, 2, "Kudu Purple Sweatshirt", "Kudu Purple Sweatshirt", "http://catalogbaseurltobereplaced/images/products/8.png", 8.5m },
                    { 9, 5, 1, "Cup<T> White Mug", "Cup<T> White Mug", "http://catalogbaseurltobereplaced/images/products/9.png", 12m },
                    { 10, 2, 3, ".NET Foundation Sheet", ".NET Foundation Sheet", "http://catalogbaseurltobereplaced/images/products/10.png", 12m },
                    { 11, 2, 3, "Cup<T> Sheet", "Cup<T> Sheet", "http://catalogbaseurltobereplaced/images/products/11.png", 8.5m },
                    { 12, 5, 2, "Prism White TShirt", "Prism White TShirt", "http://catalogbaseurltobereplaced/images/products/12.png", 12m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Catalog",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "CatalogBrands",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CatalogBrands",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CatalogBrands",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CatalogTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CatalogBrands",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CatalogBrands",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CatalogTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CatalogTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CatalogTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.AlterColumn<string>(
                name: "ShipToAddress_State",
                table: "Orders",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(60)",
                oldMaxLength: 60);

            migrationBuilder.AlterColumn<string>(
                name: "ItemOrdered_ProductName",
                table: "OrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ItemOrdered_PictureUri",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "ItemOrdered_CatalogItemId",
                table: "OrderItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Catalog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
