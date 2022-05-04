using api.Models;
using Microsoft.Data.Sqlite;
using Dapper;

namespace api.Repositories;

public class FencerRepository
{
    private readonly SqliteConnection DbConnection;
    
    public FencerRepository()
    {
        DbConnection = new SqliteConnection("Data source=ghsfl_dev.db");
        DbConnection.Open();    
    }

    public async Task<List<Fencer>> GetAllFencersForSchool(string school)
    {
        return (await DbConnection.QueryAsync<Fencer>(
            @"
                    SELECT
                        firstname, 
                        lastname, 
                        school,
                        tournaments_attended 'TournamentsAttended',
                        points
                    FROM
                        fencers
                    WHERE
                        school = @School COLLATE NOCASE
                ", new {School = school})).ToList();
    }
    
    /// <summary>
    /// returns all fencers for a specific school for a specific round
    /// </summary>
    /// <param name="schoolName">the name of the school to get the fencers for</param>
    /// <param name="round">the round to get the fencers for</param>
    /// <returns>a list of fencers</returns>
    public async Task<List<Fencer>> GetFencersForSchoolForRoundForGender(string schoolName, int round, string gender)
    {
        return (await DbConnection.QueryAsync<Fencer>(
            @"
                SELECT 
                    f.firstname,
                    f.lastname,
                    f.school,
                    f.gender
                FROM FencerRounds fr
                    INNER JOIN Fencers f ON fr.fencer_id = f.id
                WHERE
                    fr.round = @Round AND 
                    f.school = @School COLLATE NOCASE AND
                    f.gender = @Gender
            ", new {Round = round, School = schoolName, Gender = gender})).ToList();
    }

    /// <summary>
    /// gets a list of fencers from the database that match the given parameters
    /// </summary>
    /// <param name="firstname">the firstname to search for</param>
    /// <param name="lastname">the lastname to search for</param>
    /// <param name="school">the school to search for</param>
    /// <returns></returns>
    public async Task<Fencer> GetFencerFromDb(string firstname, string lastname, string school)
    {
         return await DbConnection.ExecuteScalarAsync<Fencer>(
        @"
                SELECT
                    firstname, lastname, school, tournaments_attended
                FROM
                    Fencers
                WHERE
                    firstname = @First COLLATE NOCASE 
                  and 
                    lastname = @Last COLLATE NOCASE
                  and
                    school = @School COLLATE NOCASE
            ", new {First = firstname, Last = lastname, School = school});
    }
}