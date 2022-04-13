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
    [Authorize]
    [Route("api/{school}/fencers/{round}")]
    public async Task<IActionResult> GetFencers(string school, int round)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        if (!school.ToLower().Equals(schoolClaim.ToLower()))
            return Unauthorized();

        List<Fencer> result = await repo.GetFencersForSchoolForRound(schoolClaim, round);
        
        if (result.Count == 0)
            return NotFound($"No fencers were found for: {schoolClaim}");
        
        return Ok(result);
    }
}

