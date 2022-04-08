using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Repositories;

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
    public async Task<IActionResult> Get()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Unauthorized();
        
        var handler = new JwtSecurityTokenHandler();
        var rawJwt = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        JwtSecurityToken jwt = handler.ReadJwtToken(rawJwt);
        string schoolName = jwt.Claims.Last(claim => claim.Type == "Groups").Value;

        List<Fencer> result = await repo.GetFencersForSchool(schoolName);
        
        if (result.Count == 0)
            return NotFound($"No fencers were found for: {schoolName}");
        
        return Ok();
    }
}

