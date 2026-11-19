using BuildingManagementMvc.Services;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace BuildingManagementMvc.Controllers;
[Authorize(Roles="superadmin")]
public sealed class AccountManagementController:Controller
{
 private readonly AccountManagementService _service;public AccountManagementController(AccountManagementService service)=>_service=service;
 [HttpGet]public async Task<IActionResult> Index(string? search,string? role,string? status,int page=1,int pageSize=25,CancellationToken cancellationToken=default){if(User.HasClaim("impersonated","true"))return Forbid();return View(await _service.GetPageAsync(search,role,status,page,pageSize,cancellationToken));}
}