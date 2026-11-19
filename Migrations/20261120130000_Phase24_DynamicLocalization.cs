using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace BuildingManagementMvc.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261120130000_Phase24_DynamicLocalization")]
public sealed class Phase24_DynamicLocalization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Languages", table => new
        {
            CultureCode = table.Column<string>(type:"nvarchar(20)",maxLength:20,nullable:false),
            EnglishName = table.Column<string>(type:"nvarchar(100)",maxLength:100,nullable:false),
            NativeName = table.Column<string>(type:"nvarchar(100)",maxLength:100,nullable:false),
            Direction = table.Column<string>(type:"nvarchar(12)",maxLength:12,nullable:false),
            FallbackCulture = table.Column<string>(type:"nvarchar(20)",maxLength:20,nullable:true),
            Flag = table.Column<string>(type:"nvarchar(8)",maxLength:8,nullable:true),
            IsEnabled = table.Column<bool>(type:"bit",nullable:false),
            IsDefault = table.Column<bool>(type:"bit",nullable:false),
            SortOrder = table.Column<int>(type:"int",nullable:false),
            UpdatedAtUtc = table.Column<DateTime>(type:"datetime2",nullable:false),
            UpdatedBy = table.Column<string>(type:"nvarchar(128)",maxLength:128,nullable:true)
        }, constraints: table => table.PrimaryKey("PK_Languages",x=>x.CultureCode));
        migrationBuilder.CreateTable("TranslationEntries", table => new
        {
            Key = table.Column<string>(type:"nvarchar(200)",maxLength:200,nullable:false),
            Category = table.Column<string>(type:"nvarchar(40)",maxLength:40,nullable:false),
            SourceGroup = table.Column<string>(type:"nvarchar(160)",maxLength:160,nullable:false),
            Description = table.Column<string>(type:"nvarchar(500)",maxLength:500,nullable:true),
            IsArchived = table.Column<bool>(type:"bit",nullable:false),
            UpdatedAtUtc = table.Column<DateTime>(type:"datetime2",nullable:false),
            UpdatedBy = table.Column<string>(type:"nvarchar(128)",maxLength:128,nullable:true),
            RowVersion = table.Column<byte[]>(type:"rowversion",rowVersion:true,nullable:false)
        }, constraints: table => table.PrimaryKey("PK_TranslationEntries",x=>x.Key));
        migrationBuilder.CreateTable("TranslationValues", table => new
        {
            Key = table.Column<string>(type:"nvarchar(200)",maxLength:200,nullable:false),
            CultureCode = table.Column<string>(type:"nvarchar(20)",maxLength:20,nullable:false),
            Value = table.Column<string>(type:"nvarchar(4000)",maxLength:4000,nullable:false),
            UpdatedAtUtc = table.Column<DateTime>(type:"datetime2",nullable:false),
            UpdatedBy = table.Column<string>(type:"nvarchar(128)",maxLength:128,nullable:true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_TranslationValues",x=>new{x.Key,x.CultureCode});
            table.ForeignKey("FK_TranslationValues_TranslationEntries_Key",x=>x.Key,"TranslationEntries","Key",onDelete:ReferentialAction.Cascade);
            table.ForeignKey("FK_TranslationValues_Languages_CultureCode",x=>x.CultureCode,"Languages","CultureCode",onDelete:ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_TranslationValues_CultureCode","TranslationValues","CultureCode");
        migrationBuilder.CreateIndex("IX_TranslationEntries_Category_SourceGroup","TranslationEntries",new[]{"Category","SourceGroup"});
        migrationBuilder.Sql("INSERT INTO Languages (CultureCode,EnglishName,NativeName,Direction,FallbackCulture,Flag,IsEnabled,IsDefault,SortOrder,UpdatedAtUtc) VALUES ('ar','Arabic','العربية','RTL',NULL,N'🇪🇬',1,1,1,SYSUTCDATETIME()),('en','English','English','LTR','ar',N'🇬🇧',1,0,2,SYSUTCDATETIME()),('fr','French','Français','LTR','en',N'🇫🇷',1,0,3,SYSUTCDATETIME()),('de','German','Deutsch','LTR','en',N'🇩🇪',1,0,4,SYSUTCDATETIME());");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("TranslationValues");
        migrationBuilder.DropTable("TranslationEntries");
        migrationBuilder.DropTable("Languages");
    }
}
