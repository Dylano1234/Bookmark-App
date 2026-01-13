using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.DataAccess
{
    public class GenreRepository
    {
        public List<Models.Genre> GetAll()
        {
            var result = new List<Models.Genre>();
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(DbConfig.ConnectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT id, name FROM genres;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Models.Genre
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1)
                });
            }
            return result;
        }
    }
}
