using System.IdentityModel.Tokens.Jwt;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace api.Controllers;

[ApiController]
public class FencerController : ControllerBase
{
    private readonly ILogger<RosterController> _logger;
    private SqliteConnection DB_CONNECTION; 
    
    public FencerController(ILogger<RosterController> logger)
    {
        _logger = logger;
        DB_CONNECTION = new SqliteConnection("Data source=ghsfl_dev.db");
        DB_CONNECTION.Open();
    }

    [HttpGet]
    [Route("api/fencer")]
    public IActionResult Get(string first, string last)
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Unauthorized();

        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwt = handler.ReadJwtToken(Request.Headers["Authorization"]);
        
        var f = new Fencer
        {
            FirstName = "Jimmy",
            LastName = "Wallace",
            School = "Fellowship",
            TournamentsAttended = 7
        };
        return Ok(f);
    }

    private List<Fencer> GetFencerFromDb(string firstname, string lastname, string school)
    {
        List<Fencer> fencers = new List<Fencer>();
        var command = DB_CONNECTION.CreateCommand();
        command.CommandText =
            @"
                SELECT
                    tournaments_attended
                FROM
                    Fencers
                WHERE
                    firstname = @first and 
                    lastname = @last and
                    school = @school
            ";
        command.Parameters.AddWithValue("first", firstname);
        command.Parameters.AddWithValue("last", lastname);
        command.Parameters.AddWithValue("school", school);

        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            fencers.Add(new Fencer
            {
                FirstName = lastname,
                LastName = firstname,
                School = school,
                TournamentsAttended = reader.GetInt32(0)
            });
        }

        return fencers;
    }
}