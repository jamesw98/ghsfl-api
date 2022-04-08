using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace api.Controllers;

[ApiController]
public class SchoolController : ControllerBase
{
    private SchoolRepository repo;

    public SchoolController()
    {
        repo = new SchoolRepository();
    }

    [HttpGet]
    [Route("api/school/fencers")]
    // [Authorize]
    public async Task<IActionResult> GetFencers()
    {
        // if (!Request.Headers.ContainsKey("Authorization"))
        //     return Unauthorized();

        var handler = new JwtSecurityTokenHandler();
        var rawJwt = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        JwtSecurityToken jwt = handler.ReadJwtToken(rawJwt);
        string schoolName = jwt.Claims.Last(claim => claim.Type == "Groups").Value;
        
        var temp = HttpContext.User.Identities.First().Claims.Single();

        List<Fencer> result = await repo.GetFencersForSchool(schoolName);
        
        if (result.Count == 0)
            return NotFound($"No fencers were found for: {schoolName}");
        
        return Ok(result);
    }

    // [HttpGet]
    // [Route("api/school/rosters")]
    // public async Task<IActionResult> GetRosters()
    // {
    //     
    // }
}

