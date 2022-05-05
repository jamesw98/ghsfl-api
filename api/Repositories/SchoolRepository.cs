using api.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace api.Repositories;

public class SchoolRepository
{
    private SqliteConnection DbConnection;
    private IConfiguration _config;

    public SchoolRepository(IConfiguration config)
    {
        _config = config;
        
        var connectionString = _config.GetValue<string>("DbConnection");
        
        DbConnection = new SqliteConnection($"Data source={connectionString}");
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