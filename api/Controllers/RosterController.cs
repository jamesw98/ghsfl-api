using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers;

[ApiController]
public class RosterController : ControllerBase
{
    private RosterRepository repo;
    
    public RosterController()
    {
        repo = new RosterRepository();
    }
    
    [HttpPost]
    [Route("api/roster")]
    [Authorize]
    public async Task<IActionResult> PostRosterFile()
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        
        RosterRepository.PostResponse result = await repo.ReadSubmittedFiles(schoolClaim, Request.Form.Files);
        
        if (result.Success) 
            return Ok(result);
        
        return BadRequest(result);
    }
}