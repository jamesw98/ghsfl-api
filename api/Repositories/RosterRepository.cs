using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using api.Models;

namespace api.Repositories;

public class RosterRepository
{
    private SqliteConnection DbConnection;
    
    public RosterRepository()
    {
        DbConnection = new SqliteConnection("Data source=ghsfl_dev.db");
        DbConnection.Open();
    }

    public class PostResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Fencer> NewFencers { get; set; }
    }
    
    public async Task<bool> AddRoundForSchool(string school, int round)
    {
        var command = DbConnection.CreateCommand();
        command.CommandText =
            @"
                INSERT INTO SchoolRounds
                    (school, round)
                VALUES
                    (@school, @round)
            ";
        command.Parameters.AddWithValue("school", school);
        command.Parameters.AddWithValue("round", round);

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (DbException e)
        {
            return false;
        }

        return true;
    }
    
    public async Task<bool> SchoolHasSubmittedForRound(string school, int round)
    {
        var command = DbConnection.CreateCommand();
        command.CommandText = 
            @"
                SELECT 
                    id 
                FROM 
                    SchoolRounds
                WHERE
                    school = @school COLLATE NOCASE
                  AND 
                    round = @round
            ";
        command.Parameters.AddWithValue("school", school);
        command.Parameters.AddWithValue("round", round);

        return await command.ExecuteScalarAsync() != null;
    }
    
    public async Task<int> RemoveFencersForRound(string school, int round)
    {
        var command = DbConnection.CreateCommand();
        command.CommandText = 
            @"
                DELETE FROM 
                    FencerRounds 
                WHERE
                    round = @round AND fencer_id IN (
                        SELECT
                            id
                        FROM 
                            Fencers
                        WHERE
                            school = @school COLLATE NOCASE
                    ) 
            ";
        command.Parameters.AddWithValue("school", school);
        command.Parameters.AddWithValue("round", round);

        return await command.ExecuteNonQueryAsync();
    } 
    
    public async Task<bool> AddFencerRound(int id, int round)
    {
        var command = DbConnection.CreateCommand();
        command.CommandText = 
            @"
                INSERT INTO FencerRounds 
                    (round, fencer_id) 
                VALUES 
                    (@round, @id)
            ";
        command.Parameters.AddWithValue("round", round);
        command.Parameters.AddWithValue("id", id);

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (DbException e)
        {
            return false;
        }

        return true;
    } 
    
    /// <summary>
    /// attempts to create a new fencer in the database
    /// </summary>
    /// <param name="firstname">the firstname of the fencer</param>
    /// <param name="lastname">the lastname of the fencer</param>
    /// <param name="school">the school of the fencer</param>
    /// <param name="gender">the gender of the fencer</param>
    /// <returns>the id of the newly created fencer, or -1 if the fencer already exists</returns>
    public async Task<Fencer> CreateNewFencer(string firstname, string lastname, string school, string gender)
    {
        var command = DbConnection.CreateCommand();
        command.CommandText = 
            @"
                INSERT INTO Fencers
                    (firstname, lastname, school, gender, tournaments_attended)                
                VALUES
                    (@firstname, @lastname, @school, @gender, 0);
                SELECT last_insert_rowid();
            ";
        command.Parameters.AddWithValue("firstname", firstname);
        command.Parameters.AddWithValue("lastname", lastname);
        command.Parameters.AddWithValue("school", school);
        command.Parameters.AddWithValue("gender", gender);

        await command.ExecuteNonQueryAsync();
        
        return new Fencer
        {
            FirstName = firstname,
            LastName = lastname,
            School = school,
            Gender = gender,
            TournamentsAttended = 0
        };
    }

    public async Task<int> CheckFencerExists(string firstname, string lastname, string school)
    {
        var command = DbConnection.CreateCommand();
        command.CommandText = 
            @"
                SELECT 
                    id
                FROM
                    Fencers
                WHERE
                    firstname = @firstname COLLATE NOCASE 
                  AND 
                      lastname = @lastname COLLATE NOCASE 
                  AND 
                      school = @school COLLATE NOCASE
            ";
        command.Parameters.AddWithValue("lastname", lastname);
        command.Parameters.AddWithValue("firstname", firstname);
        command.Parameters.AddWithValue("school", school);

        int id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return id;
    }
    
    /// <summary>
    /// checks if a school exists in the database
    /// </summary>
    /// <param name="school">the school to search for</param>
    /// <returns>true if the school was found, false if not</returns>
    public async Task<bool> CheckSchoolExists(string school)
    {
        var command = DbConnection.CreateCommand();
        command.CommandText = 
            @"
                SELECT 
                    school
                FROM
                    Schools
                WHERE
                    school = @school COLLATE NOCASE
            ";
        command.Parameters.AddWithValue("school", school);
        
        return await command.ExecuteScalarAsync() != null;
    }
    
    public async Task<List<Fencer>> ReadRosterFile(string filename, string school, string gender, int round)
    {
        List<Fencer> result = new List<Fencer>();
        
        using (var read = new StreamReader($"rosters/{filename}")) 
        {
            await read.ReadLineAsync();
            while (!read.EndOfStream)
            {
                string line = await read.ReadLineAsync();
                string[] values = line.Split(",");
                
                string last = values[0];
                string first = values[1];

                int fencerId = await CheckFencerExists(last, first, school); 
                
                if (fencerId != -1) 
                {
                    result.Add(await CreateNewFencer(last, first, school, gender));
                }

                await AddFencerRound(fencerId, round);
            }
        }

        return result;
    }

    private async Task<bool> UpdateSchoolRound(string school, int round)
    {
        if (await SchoolHasSubmittedForRound(school, round))
        {
            await RemoveFencersForRound(school, round);
        }

        return await AddRoundForSchool(school, round);
    }

    public async Task<PostResponse> ReadSubmittedFiles(string school, IFormFileCollection files)
    {
        PostResponse pr = new PostResponse();
        pr.NewFencers = new List<Fencer>();

        foreach (IFormFile f in files)
        {
            Regex filenameRegex = new Regex(@"r(\d+)_([a-zA-Z]+)_([male|female|Male|Female]+).csv");

            var matches = filenameRegex.Match(f.FileName);
            if (matches.Groups.Count != 4)
            {
                pr.Message = "Error: invalid filename";
                pr.Success = false;
                return pr;
            }
            
            int round = Convert.ToInt32(matches.Groups[1].Value);
            string gender = matches.Groups[3].Value.ToLower().Substring(0,1);

            if (!await CheckSchoolExists(school))
            {
                pr.Message = $"Error: could not find school: {school} in database";
                pr.Success = false;
                return pr;
            }
            
            // write contents of request form to a file stored locally
            using (Stream fs = new FileStream($"rosters/{f.FileName}", FileMode.Create, FileAccess.Write)) 
            {
                await f.CopyToAsync(fs);
            }

            await UpdateSchoolRound(school, round);

            pr.NewFencers = await ReadRosterFile(f.FileName, school, gender, round);
        }

        pr.Message = "Success!";
        pr.Success = true;
        return pr;
    }
}