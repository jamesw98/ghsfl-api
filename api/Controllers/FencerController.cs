using System.IdentityModel.Tokens.Jwt;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace api.Controllers;

[ApiController]
public class FencerController : ControllerBase
{
    private FencerRepository repo;
    
    public FencerController()
    {
        repo = new FencerRepository();
    }

    [HttpGet]
    [Route("api/fencer")]
    public IActionResult Get(string first, string last, string school)
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Unauthorized();
        
        // just testing, no real auth as of yet
        var handler = new JwtSecurityTokenHandler();
        var rawJwt = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        JwtSecurityToken jwt = handler.ReadJwtToken(rawJwt);
        string requestClaim = jwt.Claims.Last(claim => claim.Type == "Groups").Value;
        
        if (school.ToLower() != requestClaim && !requestClaim.Equals("admin"))
            return Unauthorized();

        List<Fencer> result = repo.GetFencersFromDB(first, last, school);

        if (result.Count == 0)
            return NotFound();
        
        return Ok(result);
    }
}