using System.IdentityModel.Tokens.Jwt;
using api.Models;
using api.Repositories;
using Microsoft.AspNetCore.Authorization;
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
    [Route("api/{school}/fencer")]
    [Authorize]
    public async Task<IActionResult> GetFencersAllForSchool(string school)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;

        if (!school.Equals(schoolClaim))
            return Unauthorized();
        
        // List<Fencer>
        return Ok();
    }

    [HttpGet]
    [Route("api/fencer")]
    [Authorize]
    public async Task<IActionResult> GetFencer(string first, string last)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        
        Fencer result = await repo.GetFencerFromDb(first, last, schoolClaim);

        return result is not null ? Ok(result) : NotFound();
    }
}