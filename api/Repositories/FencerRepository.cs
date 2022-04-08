using api.Models;
using Microsoft.Data.Sqlite;

namespace api.Repositories;

public class FencerRepository
{
    private SqliteConnection DbConnection;
    
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
    public List<Fencer> GetFencersFromDB(string firstname, string lastname, string school)
    {
        List<Fencer> fencers = new List<Fencer>();
        var command = DbConnection.CreateCommand();
        command.CommandText =
            @"
                SELECT
                    firstname, lastname, school, tournaments_attended
                FROM
                    Fencers
                WHERE
                    firstname = @first COLLATE NOCASE 
                  and 
                    lastname = @last COLLATE NOCASE
                  and
                    school = @school COLLATE NOCASE
            ";
        command.Parameters.AddWithValue("first", firstname);
        command.Parameters.AddWithValue("last", lastname);
        command.Parameters.AddWithValue("school", school);

        var reader = command.ExecuteReader();
        while (reader.Read())
        {
            fencers.Add(new Fencer
            {
                FirstName = reader.GetString(0),
                LastName = reader.GetString(1),
                School = reader.GetString(2),
                TournamentsAttended = reader.GetInt32(3)
            });
        }

        return fencers;
    }
}