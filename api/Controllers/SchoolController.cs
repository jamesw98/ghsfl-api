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
    private IConfiguration _config;

    public SchoolController(IConfiguration config)
    {
        _config = config;
        repo = new SchoolRepository(_config);
    }
    
    [HttpGet]
    [Route("api/{school}")]
    [Authorize]
    public async Task<IActionResult> GetRounds(string school)
    {
        //TODO
        string schoolClaim = HttpContext.User.Identities.First().Claims.Last().Value;
        return Ok();
    }
}

