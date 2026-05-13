using Bookmark_App.Models;
using Microsoft.Data.Sqlite;

namespace Bookmark_App.DataAccess
{
    public class GenreRepository : IGenreRepository
    {
        public List<Genre> GetAll()
        {
            var result = new List<Genre>();
            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM genres;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Genre
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1)
                });
            }
            return result;
        }
    }
}
