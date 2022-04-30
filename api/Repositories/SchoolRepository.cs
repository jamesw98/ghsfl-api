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
    
    /// <summary>
    /// returns all fencers that have been submitted in a roster for the given school
    /// </summary>
    /// <param name="schoolName">the name of the school to get the fencers for</param>
    /// <returns>a list of fencers</returns>
    public async Task<List<Fencer>> GetFencersForSchool(string schoolName)
    {
        return (await DbConnection.QueryAsync<Fencer>(
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
                        school = @School COLLATE NOCASE
                ", new {School = schoolName})).ToList();
    } 
}