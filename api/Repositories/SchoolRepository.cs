using api.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace api.Repositories;

public class SchoolRepository
{
    private SqliteConnection DbConnection;

    public SchoolRepository()
    {
        DbConnection = new SqliteConnection("Data source=ghsfl_dev.db");
        DbConnection.Open();
    }

    public async Task<List<Fencer>> GetFencersForSchoolForRound(string schoolName, int round)
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
                    f.school = @School COLLATE NOCASE
            ", new {Round = round, School = schoolName})).ToList();
        }
    
    /// <summary>
    /// returns all fencers that have been submitted in a roster for the given school
    /// </summary>
    /// <param name="schoolName">the name of the school to get the fencers for</param>
    /// <returns>a list of fencers</returns>
    public async Task<List<Fencer>> GetFencersForSchool(string schoolName)
    {
        List<Fencer> fencers = new List<Fencer>();
        
        var command = DbConnection.CreateCommand();
        command.CommandText =
            @"
                SELECT
                    firstname, 
                    lastname, 
                    tournaments_attended,
                    points,
                    gender,
                    school
                FROM
                    Fencers
                WHERE 
                    school = @school COLLATE NOCASE
            ";
        command.Parameters.AddWithValue("school", schoolName);

        var reader = await command.ExecuteReaderAsync();

        while (reader.Read())
        {
            fencers.Add(new Fencer()
            {
                FirstName = reader.GetString(0),
                LastName = reader.GetString(1),
                TournamentsAttended = reader.GetInt32(2),
                Points = reader.GetInt32(3),
                Gender = reader.GetString(4)[0] == 'F' ? "Female" : "Male",
                School = reader.GetString(5)
            });
        }

        return fencers;
    } 
}