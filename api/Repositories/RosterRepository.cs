using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using api.Models;
using Dapper;

namespace api.Repositories;

public class RosterRepository
{
    private IConfiguration _config;
    private SqliteConnection DbConnection;
    private int[] _rounds;
    
    public RosterRepository(IConfiguration config)
    {
        _config = config;

        var connectionString = _config.GetValue<string>("DbConnection");
        
        DbConnection = new SqliteConnection($"Data source={connectionString}");
        DbConnection.Open();
        
        _rounds = new[] {1, 2, 3, 4, 5, 6, 7, 8, 9};
    }

    public class PostResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<Fencer> NewFencers { get; set; }
    }

    public class RosterStatus
    {
        public int Round { get; set; }
        public bool SubmittedMale { get; set; }
        public bool SubmittedFemale { get; set; }
    }
    
    /// <summary>
    /// returns the submission status of every roster for a specified school 
    /// </summary>
    /// <param name="school">the school to query for</param>
    /// <returns>a list of roster submission objects</returns>
    public async Task<List<RosterStatus>> GetAllRostersStatus(string school)
    {
        List<RosterStatus> rosters = new List<RosterStatus>();
        List<RosterStatus> rostersFromDb = (await DbConnection.QueryAsync<RosterStatus>(
            @"
                    SELECT
                        round,
                        male 'SubmittedMale',
                        female 'SubmittedFemale'
                    FROM
                        SchoolRounds
                    WHERE
                        school = @School collate NOCASE
                    ORDER BY round                                                 
                ", new {School = school})).ToList();

        foreach (int i in _rounds)
        {
            if (rostersFromDb.All(r => r.Round != i))
                rosters.Add(new RosterStatus
                {   
                    Round = i,
                    SubmittedFemale = false,
                    SubmittedMale = false
                });
            else
                rosters.Add(rostersFromDb.First(r => r.Round == i));
        }

        return rosters;
    }
    
    /// <summary>
    /// adds a round to a school in the database
    /// </summary>
    /// <param name="school">the school to add a round to</param>
    /// <param name="round">the round to add to the school</param>
    /// <param name="gender">the gender of the fencers in this roster</param>
    private async Task AddRoundForSchool(string school, int round, char gender)
    {
        string genderColumn = gender is 'f' or 'F' ? "Female" : "Male";

        var hasSubmittedForRound = await DbConnection.QueryAsync<int>(
            @"
                    SELECT 
                        round 
                    FROM SchoolRounds 
                    WHERE 
                        school = @School COLLATE NOCASE AND 
                        round = @Round
                ", new {School = school.ToLower(), Round = round});
        
        if (!hasSubmittedForRound.Any())
        {
            await DbConnection.ExecuteAsync(
                @$"
                    INSERT INTO SchoolRounds
                        (school, round, {genderColumn})
                    VALUES
                        (@School, @Round, @GenderVal)
                ", new {School = school.ToLower(), Round = round, GenderVal = true});
        }
        else
        {
            await DbConnection.ExecuteAsync(
                @$"
                    UPDATE SchoolRounds
                    SET {genderColumn} = @GenderVal
                    WHERE 
                        school = @School COLLATE NOCASE AND 
                        round = @Round
                ", new {School = school, Round = round, GenderVal = true});
        }
    }
    
    /// <summary>
    /// checks if a school has submitted roster(s) for a specific round
    /// </summary>
    /// <param name="school">the school to check</param>
    /// <param name="round">the round to check</param>
    /// <returns>true if they have, false if they haven't</returns>
    public async Task<RosterStatus> SchoolHasSubmittedForRound(string school, int round)
    {
        var result = await DbConnection.QueryAsync<RosterStatus>(
            @"SELECT 
                    round, 
                    male 'SubmittedMale', 
                    female 'SubmittedFemale' 
                FROM 
                    SchoolRounds
                WHERE
                    school = @School COLLATE NOCASE
                  AND 
                    round = @Round
                ", new {School = school, Round = round});
        return result.Count() != 0 ? result.First() : null;
    }
    
    /// <summary>
    /// removes all fencers for a specific school for a specific round
    /// </summary>
    /// <param name="school">the school to remove from</param>
    /// <param name="round">the round to remove from</param>
    /// <returns>the number of fencers removed</returns>
    public async Task RemoveFencersForRound(string school, int round, char gender)
    {
        await DbConnection.ExecuteAsync(
            @"
                    DELETE FROM 
                        FencerRounds 
                    WHERE
                        round = @Round AND fencer_id IN (
                            SELECT
                                id
                            FROM 
                                Fencers
                            WHERE
                                school = @School COLLATE NOCASE AND
                                gender = @Gender
                            )
                ", new {School = school, Round = round, Gender = gender});
    } 
    
    /// <summary>
    /// adds to the FencerRounds table
    /// </summary>
    /// <param name="id">id of the fencer to add</param>
    /// <param name="round">round to add</param>
    public async Task AddFencerRound(int id, int round)
    {
        await DbConnection.ExecuteAsync(
            @"
                    INSERT INTO FencerRounds 
                        (round, fencer_id) 
                    VALUES 
                        (@Round, @Id)
                ", new {Round = round, Id = id});
    } 
    
    /// <summary>
    /// attempts to create a new fencer in the database
    /// </summary>
    /// <param name="firstname">the firstname of the fencer</param>
    /// <param name="lastname">the lastname of the fencer</param>
    /// <param name="school">the school of the fencer</param>
    /// <param name="gender">the gender of the fencer</param>
    /// <returns>the id of the newly created fencer</returns>
    public async Task<int> CreateNewFencer(string firstname, string lastname, string school, string gender)
    {
        return await DbConnection.ExecuteScalarAsync<int>(
            @"
                    INSERT INTO Fencers
                        (firstname, lastname, school, gender, tournaments_attended, points)                
                    VALUES
                        (@Firstname, @Lastname, @School, @Gender, 0, 0);
                    SELECT last_insert_rowid();
                ", new {Firstname = firstname, Lastname = lastname, School = school, Gender = gender});
    }
    
    /// <summary>
    /// check if a fencer already exists in the database
    /// </summary>
    /// <param name="firstname">the firstname to check</param>
    /// <param name="lastname">the lastname to check</param>
    /// <param name="school">the school to check</param>
    /// <returns>the id of the fencer, if there is one</returns>
    /// TODO check to see what happens if the fencer does not exist
    public async Task<int> CheckFencerExists(string firstname, string lastname, string school)
    {
        return await DbConnection.ExecuteScalarAsync<int>(
            @"
                    SELECT 
                        id
                    FROM
                        Fencers
                    WHERE
                        firstname = @Firstname COLLATE NOCASE 
                      AND 
                          lastname = @Lastname COLLATE NOCASE 
                      AND 
                          school = @School COLLATE NOCASE
                ", new {Firstname = firstname, Lastname = lastname, School = school});
    }
    
    /// <summary>
    /// checks if a school exists in the database
    /// </summary>
    /// <param name="school">the school to search for</param>
    /// <returns>true if the school was found, false if not</returns>
    public async Task<bool> CheckSchoolExists(string school)
    {
        return await DbConnection.ExecuteScalarAsync<string>(
            @"
                    SELECT 
                        school
                    FROM
                        Schools
                    WHERE
                        school = @School COLLATE NOCASE
                ", new {School = school}) != null;
    }
    
    /// <summary>
    /// reads data from the submitted roster file
    /// </summary>
    /// <param name="filename">the name of the roster to read</param>
    /// <param name="school">the school the roster was submitted for</param>
    /// <param name="gender">the gender the roster was submitted for</param>
    /// <param name="round">the round the roster was submitted for</param>
    /// <returns>a list of fencer objects representing fencers that were not already in the database</returns>
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
                
                if (fencerId == 0)
                {
                    fencerId = await CreateNewFencer(last, first, school, gender);
                    result.Add(new Fencer
                    {
                        FirstName = first, 
                        LastName = last
                    });
                }

                await AddFencerRound(fencerId, round);
            }
        }

        return result;
    }
    
    /// <summary>
    /// updates a school's roster for a specific round in the database
    /// </summary>
    /// <param name="school">the school to update</param>
    /// <param name="round">the round to update</param>
    /// <returns>true if there were no exceptions, false if not</returns>
    private async Task UpdateSchoolRound(string school, int round, char gender)
    {
        RosterStatus status = await SchoolHasSubmittedForRound(school, round);

        if (status != null)
        {
            if (gender is 'f' or 'F' && status.SubmittedFemale || gender is 'm' or 'M' && status.SubmittedMale)
                await RemoveFencersForRound(school, round, gender);    
        }

        await AddRoundForSchool(school, round, gender);
    }
    
    /// <summary>
    /// reads the submitted roster(s) and writes to the local storage
    /// also ensures that they are properly named
    /// </summary>
    /// <param name="school">the school the roster(s) are submitted for</param>
    /// <param name="files">the list of files that were uploaded</param>
    /// <returns>a PostResponse object that contains whether or not the action was a success, a message, and a list of
    /// fencers that were not already in the database</returns>
    public async Task<PostResponse> ReadSubmittedFiles(string school, IFormFileCollection files, int round)
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
            
            char gender = matches.Groups[3].Value.ToLower()[0];

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

            await UpdateSchoolRound(school, round, gender);

            pr.NewFencers = await ReadRosterFile(f.FileName, school, gender.ToString(), round);
        }

        pr.Message = "Success!";
        pr.Success = true;
        return pr;
    }
}