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
    private IConfiguration _config;
    
    public RosterController(IConfiguration config)
    {
        _config = config;
        repo = new RosterRepository(_config);
    }
    
    /// <summary>
    /// returns the status of rosters for the school
    /// </summary>
    /// <returns>a list of roster statues</returns>
    [HttpGet]
    [Route("api/roster/{school}")]
    [Authorize]
    public async Task<IActionResult> GetRostersForSchool(string school)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        
        if (!school.Equals(schoolClaim) && !schoolClaim.Equals("admin"))
            return Unauthorized();

        return Ok(await repo.GetAllRostersStatus(school));
    }

    [HttpPost]
    [Route("api/roster")]
    [Authorize]
    public async Task<IActionResult> PostRosterFile(int round)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        
        RosterRepository.PostResponse result = await repo.ReadSubmittedFiles(schoolClaim, Request.Form.Files, round);

        if (result.Success) 
            return Ok(result);
        
        return BadRequest(result);
    }
}