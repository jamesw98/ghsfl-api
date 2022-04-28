using System.IdentityModel.Tokens.Jwt;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> Get(string first, string last, string school)
    {
        //TODO get rid of this old auth, switch to new auth
        if (!Request.Headers.ContainsKey("Authorization"))
            return Unauthorized();
        
        var handler = new JwtSecurityTokenHandler();
        var rawJwt = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        JwtSecurityToken jwt = handler.ReadJwtToken(rawJwt);
        string requestClaim = jwt.Claims.Last(claim => claim.Type == "Groups").Value;
        
        if (school.ToLower() != requestClaim && !requestClaim.Equals("admin"))
            return Unauthorized();

        List<Fencer> result = await repo.GetFencersFromDB(first, last, school);

        if (result.Count == 0)
            return NotFound();
        
        return Ok(result);
    }
}