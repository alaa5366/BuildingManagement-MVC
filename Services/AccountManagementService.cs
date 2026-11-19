using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;
namespace BuildingManagementMvc.Services;
public sealed class AccountManagementService
{
 private readonly IDbContextFactory<AppDbContext> _factory;
 public AccountManagementService(IDbContextFactory<AppDbContext> factory)=>_factory=factory;
 public async Task<AccountManagementPage> GetPageAsync(string? search,string? role,string? status,int page,int pageSize,CancellationToken ct=default)
 {
  await using var db=await _factory.CreateDbContextAsync(ct); var now=DateTime.UtcNow;
  var rows=await (from a in db.UserAccounts.AsNoTracking() join p in db.Persons.AsNoTracking() on a.PersonId equals p.PersonId
   select new AccountManagementRow{AccountId=a.AccountId,Name=p.DisplayName,Phone=p.PrimaryPhone,Email=a.NormalizedEmail??p.PrimaryEmail,AccountStatus=a.AccountStatus,MustChangePin=a.MustChangePin,IsArchived=p.IsArchived,LastLoginAtUtc=a.LastLoginAtUtc}).ToListAsync(ct);
  var grants=await (from g in db.UserRoleAssignments.AsNoTracking() join d in db.RoleDefinitions.AsNoTracking() on g.RoleKey equals d.RoleKey
   where g.ValidFromUtc<=now&&(g.ValidToUtc==null||g.ValidToUtc>now)&&g.RevokedAtUtc==null&&d.IsActive select new{g.AccountId,g.RoleKey}).ToListAsync(ct);
  var roles=grants.GroupBy(x=>x.AccountId).ToDictionary(x=>x.Key,x=>string.Join(", ",x.Select(y=>y.RoleKey).Distinct()));
  var dup=rows.Where(x=>!string.IsNullOrWhiteSpace(x.Phone)).Select(x=>new{x.AccountId,Phone=AuthHelpers.NormalizePhone(x.Phone)}).Where(x=>!string.IsNullOrWhiteSpace(x.Phone)).GroupBy(x=>x.Phone!).Where(x=>x.Count()>1).SelectMany(x=>x.Select(y=>y.AccountId)).ToHashSet();
  foreach(var x in rows){x.Roles=roles.GetValueOrDefault(x.AccountId,"بدون دور نشط");x.IsDuplicatePhone=dup.Contains(x.AccountId);x.StatusLabel=x.AccountStatus switch{1=>"نشط",0=>"غير نشط",2=>"موقوف / مقفل",3=>"معلق",_=>"غير معروف"};}
  IEnumerable<AccountManagementRow> q=rows;
  if(!string.IsNullOrWhiteSpace(search)){var s=search.Trim();q=q.Where(x=>x.Name.Contains(s,StringComparison.OrdinalIgnoreCase)||(x.Phone?.Contains(s,StringComparison.OrdinalIgnoreCase)??false)||(x.Email?.Contains(s,StringComparison.OrdinalIgnoreCase)??false)||x.AccountId.ToString().Contains(s,StringComparison.OrdinalIgnoreCase));}
  if(!string.IsNullOrWhiteSpace(role))q=q.Where(x=>x.Roles.Split(", ").Contains(role,StringComparer.OrdinalIgnoreCase));
  if(status=="active")q=q.Where(x=>x.AccountStatus==1);else if(status=="inactive")q=q.Where(x=>x.AccountStatus!=1);
  var result=q.OrderBy(x=>x.Name).ToList();pageSize=Math.Clamp(pageSize,10,100);var total=result.Count;var pages=Math.Max(1,(int)Math.Ceiling(total/(double)pageSize));page=Math.Clamp(page,1,pages);
  return new(){Search=search,Role=role,Status=status,Page=page,PageSize=pageSize,Total=total,TotalPages=pages,TotalAccounts=rows.Count,ActiveAccounts=rows.Count(x=>x.AccountStatus==1),InactiveAccounts=rows.Count(x=>x.AccountStatus!=1),DuplicatePhoneAccounts=dup.Count,Rows=result.Skip((page-1)*pageSize).Take(pageSize).ToList()};
 }
}
public sealed class AccountManagementPage{public string? Search{get;init;}public string? Role{get;init;}public string? Status{get;init;}public int Page{get;init;}public int PageSize{get;init;}public int Total{get;init;}public int TotalPages{get;init;}public int TotalAccounts{get;init;}public int ActiveAccounts{get;init;}public int InactiveAccounts{get;init;}public int DuplicatePhoneAccounts{get;init;}public List<AccountManagementRow> Rows{get;init;}=new();}
public sealed class AccountManagementRow{public Guid AccountId{get;set;}public string Name{get;set;}="";public string? Phone{get;set;}public string? Email{get;set;}public byte AccountStatus{get;set;}public bool MustChangePin{get;set;}public bool IsArchived{get;set;}public bool IsDuplicatePhone{get;set;}public string Roles{get;set;}="";public string StatusLabel{get;set;}="";public DateTime? LastLoginAtUtc{get;set;}}
