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