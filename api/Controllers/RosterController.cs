using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using api.Models;
using api.Repositories;

namespace api.Controllers;

[ApiController]
public class RosterController : ControllerBase
{
    private RosterRepository repo;
    
    public RosterController()
    {
        repo = new RosterRepository();
    }

    public class PostRequest
    {
        public string School { get; set; }
    }
    
    [HttpGet]
    [Route("api/roster")]
    public IActionResult Get(int round)
    {
        return Ok(round);
    }
    
    [HttpPost]
    [Route("api/roster")]
    public async Task<IActionResult> Post()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Unauthorized();
        
        // yes, this is reused from FencerController
        // just testing, no real auth as of yet
        var handler = new JwtSecurityTokenHandler();
        var rawJwt = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        JwtSecurityToken jwt = handler.ReadJwtToken(rawJwt);
        string claimSchool = jwt.Claims.Last(claim => claim.Type == "Groups").Value;
        
        if (Request.Form.Files.Count < 1)
            return BadRequest("No files attached");
        
        RosterRepository.PostResponse result = await repo.ReadSubmittedFiles(claimSchool, Request.Form.Files);
        if (result.Success) 
            return Ok(result);
        
        return BadRequest(result);
    }
}