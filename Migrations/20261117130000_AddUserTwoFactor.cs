using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace BuildingManagementMvc.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20261117130000_AddUserTwoFactor")]
public partial class AddUserTwoFactor : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.AddColumn<bool>(name:"TwoFactorEnabled",table:"Users",type:"bit",nullable:false,defaultValue:false);
  migrationBuilder.AddColumn<string>(name:"TwoFactorSecretEncrypted",table:"Users",type:"nvarchar(512)",maxLength:512,nullable:true);
  migrationBuilder.AddColumn<string>(name:"TwoFactorRecoveryCodesJson",table:"Users",type:"nvarchar(4000)",maxLength:4000,nullable:true);
  migrationBuilder.AddColumn<long>(name:"TwoFactorLastUsedStep",table:"Users",type:"bigint",nullable:true);
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.DropColumn(name:"TwoFactorEnabled",table:"Users");
  migrationBuilder.DropColumn(name:"TwoFactorSecretEncrypted",table:"Users");
  migrationBuilder.DropColumn(name:"TwoFactorRecoveryCodesJson",table:"Users");
  migrationBuilder.DropColumn(name:"TwoFactorLastUsedStep",table:"Users");
 }
}