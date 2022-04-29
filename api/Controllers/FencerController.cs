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
    [Route("api/fencer")]
    [Authorize]
    public async Task<IActionResult> GetFencer(string first, string last)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        
        List<Fencer> result = await repo.GetFencersFromDB(first, last, schoolClaim);

        if (result.Count == 0)
            return NotFound();
        
        return Ok(result);
    }
}