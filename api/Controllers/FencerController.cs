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
    private IConfiguration _config;
    
    public FencerController(IConfiguration config)
    {
        _config = config;
        repo = new FencerRepository(_config);
    }

    [HttpGet]
    [Route("api/fencer/{school}")]
    [Authorize]
    public async Task<IActionResult> GetAllFencersForSchool(string school)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value.ToLower();
        
        if (!school.Equals(schoolClaim) && !schoolClaim.Equals("admin"))
            return Unauthorized();

        List<Fencer> result = await repo.GetAllFencersForSchool(schoolClaim);
        return Ok(result);
    }
    
    [HttpGet]
    [Route("api/fencer/{school}/{round}/{gender}")]
    [Authorize]
    public async Task<IActionResult> GetFencersForRound(string school, int round, string gender)
    {
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        
        if (!school.Equals(schoolClaim) && !schoolClaim.Equals("admin"))
            return Unauthorized();

        List<Fencer> result = await repo.GetFencersForSchoolForRoundForGender(schoolClaim, round, gender);

        return Ok(result); 
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