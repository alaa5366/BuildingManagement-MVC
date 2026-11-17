using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;
namespace BuildingManagementMvc.Services;
/// <summary>Manages opt-in TOTP two-factor authentication for administrator accounts.</summary>
public sealed class TwoFactorService
{
 private readonly IDbContextFactory<AppDbContext> _factory;
 private readonly IEncryptionService _encryption;
 private readonly ILogger<TwoFactorService> _logger;
 public TwoFactorService(IDbContextFactory<AppDbContext> factory,IEncryptionService encryption,ILogger<TwoFactorService> logger)
 { _factory=factory;_encryption=encryption;_logger=logger; }
 public static string CreateSecret()=>TotpAuthenticator.GenerateSecret();
 public static string CreateProvisioningUri(string secret,string accountName,string issuer="BuildingManagement")
 { var label=Uri.EscapeDataString(issuer+":"+accountName);return $"otpauth://totp/{label}?secret={Uri.EscapeDataString(secret)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30"; }
 public async Task<bool> IsEnabledAsync(string uid,CancellationToken ct=default)
 { if(string.IsNullOrWhiteSpace(uid))return false;await using var db=await _factory.CreateDbContextAsync(ct);return await db.Users.AsNoTracking().AnyAsync(x=>x.Uid==uid&&(x.Role=="admin"||x.Role=="superadmin")&&x.TwoFactorEnabled,ct); }
 public async Task<IReadOnlyList<string>?> EnableAsync(string uid,string secret,string code,CancellationToken ct=default)
 {
  if(string.IsNullOrWhiteSpace(uid)||string.IsNullOrWhiteSpace(secret))return null;
  using var gate=await KeyedLock.AcquireAsync("2fa:"+uid,ct);await using var db=await _factory.CreateDbContextAsync(ct);
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Uid==uid,ct);if(user==null||user.Role is not("admin" or "superadmin")||user.TwoFactorEnabled)return null;
  if(!TotpAuthenticator.TryVerify(secret,code,DateTimeOffset.UtcNow,out var step))return null;
  var codes=Enumerable.Range(0,10).Select(_=>FormatCode(TotpAuthenticator.EncodeBase32(RandomNumberGenerator.GetBytes(10)))).ToArray();
  user.TwoFactorSecretEncrypted=_encryption.Encrypt(secret);user.TwoFactorEnabled=true;user.TwoFactorLastUsedStep=step;
  user.TwoFactorRecoveryCodesJson=JsonSerializer.Serialize(codes.Select(HashCode));user.UpdatedAt=DateTime.UtcNow;
  await db.SaveChangesAsync(ct);return codes;
 }
 public async Task<bool> VerifyAndConsumeAsync(string uid,string code,CancellationToken ct=default)
 {
  if(string.IsNullOrWhiteSpace(uid)||string.IsNullOrWhiteSpace(code))return false;
  using var gate=await KeyedLock.AcquireAsync("2fa:"+uid,ct);await using var db=await _factory.CreateDbContextAsync(ct);
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Uid==uid,ct);
  if(user==null||!user.TwoFactorEnabled||string.IsNullOrWhiteSpace(user.TwoFactorSecretEncrypted))return false;
  string secret;try{secret=_encryption.Decrypt(user.TwoFactorSecretEncrypted);}catch(Exception ex){_logger.LogError(ex,"Unable to decrypt 2FA secret for {Uid}",uid);return false;}
  if(TotpAuthenticator.TryVerify(secret,code,DateTimeOffset.UtcNow,out var step))
  { if(user.TwoFactorLastUsedStep.HasValue&&step<=user.TwoFactorLastUsedStep.Value)return false;user.TwoFactorLastUsedStep=step;await db.SaveChangesAsync(ct);return true; }
  var hashes=ReadHashes(user.TwoFactorRecoveryCodesJson);var candidate=HashCode(code);var match=-1;
  for(var i=0;i<hashes.Count;i++)if(FixedEquals(hashes[i],candidate))match=i;
  if(match<0)return false;hashes.RemoveAt(match);user.TwoFactorRecoveryCodesJson=JsonSerializer.Serialize(hashes);await db.SaveChangesAsync(ct);return true;
 }
 public async Task<bool> DisableAsync(string uid,string code,CancellationToken ct=default)
 {
  if(!await VerifyAndConsumeAsync(uid,code,ct))return false;
  using var gate=await KeyedLock.AcquireAsync("2fa:"+uid,ct);await using var db=await _factory.CreateDbContextAsync(ct);
  var user=await db.Users.FirstOrDefaultAsync(x=>x.Uid==uid,ct);if(user==null)return false;
  user.TwoFactorEnabled=false;user.TwoFactorSecretEncrypted=null;user.TwoFactorRecoveryCodesJson=null;user.TwoFactorLastUsedStep=null;user.UpdatedAt=DateTime.UtcNow;
  await db.SaveChangesAsync(ct);return true;
 }
 private static List<string> ReadHashes(string? json){if(string.IsNullOrWhiteSpace(json))return new();try{return JsonSerializer.Deserialize<List<string>>(json)??new();}catch(JsonException){return new();}}
 private static string FormatCode(string s)=>string.Join("-",Enumerable.Range(0,(s.Length+3)/4).Select(i=>s.Substring(i*4,Math.Min(4,s.Length-i*4))));
 private static string HashCode(string code){var n=new string((code??"").ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(n)));}
 private static bool FixedEquals(string a,string b){try{return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(a),Convert.FromHexString(b));}catch(FormatException){return false;}}
}
internal static class TotpAuthenticator
{
 private const string Alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
 internal static string GenerateSecret(int bytes=20)=>EncodeBase32(RandomNumberGenerator.GetBytes(bytes));
 internal static string GenerateCode(string secret,DateTimeOffset time)
 {
  var key=DecodeBase32(secret);var counter=(ulong)(time.ToUnixTimeSeconds()/30);Span<byte> msg=stackalloc byte[8];
  for(var i=7;i>=0;i--){msg[i]=(byte)(counter&255);counter>>=8;}
  using var hmac=new HMACSHA1(key);var hash=hmac.ComputeHash(msg.ToArray());var offset=hash[^1]&15;
  var binary=((hash[offset]&127)<<24)|((hash[offset+1]&255)<<16)|((hash[offset+2]&255)<<8)|(hash[offset+3]&255);
  return (binary%1000000).ToString("D6",System.Globalization.CultureInfo.InvariantCulture);
 }
 internal static bool TryVerify(string secret,string? code,DateTimeOffset time,out long accepted)
 {
  accepted=-1;if(string.IsNullOrWhiteSpace(code)||code.Length!=6||code.Any(c=>c<'0'||c>'9'))return false;
  var current=time.ToUnixTimeSeconds()/30;var supplied=Encoding.ASCII.GetBytes(code);
  for(var step=current-1;step<=current+1;step++){if(step<0)continue;var candidate=Encoding.ASCII.GetBytes(GenerateCode(secret,DateTimeOffset.FromUnixTimeSeconds(step*30)));if(CryptographicOperations.FixedTimeEquals(candidate,supplied)){accepted=step;return true;}}
  return false;
 }
 internal static string EncodeBase32(ReadOnlySpan<byte> data)
 {
  var sb=new StringBuilder();var buffer=0;var bits=0;foreach(var b in data){buffer=(buffer<<8)|b;bits+=8;while(bits>=5){sb.Append(Alphabet[(buffer>>(bits-5))&31]);bits-=5;}}
  if(bits>0)sb.Append(Alphabet[(buffer<<(5-bits))&31]);return sb.ToString();
 }
 internal static byte[] DecodeBase32(string text)
 {
  var value=new string((text??"").Where(c=>!char.IsWhiteSpace(c)&&c!='=').ToArray()).ToUpperInvariant();var output=new List<byte>();var buffer=0;var bits=0;
  foreach(var c in value){var n=Alphabet.IndexOf(c);if(n<0)throw new FormatException("Invalid Base32 secret.");buffer=(buffer<<5)|n;bits+=5;if(bits>=8){output.Add((byte)((buffer>>(bits-8))&255));bits-=8;}}
  return output.ToArray();
 }
}