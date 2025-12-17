using Bookmark_App.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.DataAccess
{
    public class ListRepository
    {
        public List<List> GetAll()
        {
            var result = new List<List>();
            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            // get lists with item counts (single query)
            cmd.CommandText = @"
SELECT l.id, l.title, l.cover_image, COUNT(i.id) as item_count
FROM lists l
LEFT JOIN items i ON i.list_id = l.id
GROUP BY l.id
ORDER BY l.title;
";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var list = new List
                {
                    id = reader.GetInt32(0),
                    title = reader.GetString(1),
                    coverImage = reader.IsDBNull(2) ? null : (byte[])reader["cover_image"],
                };

                // set the cached count so UI can show it immediately
                list.itemCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                result.Add(list);
            }

            return result;
        }

        public int Insert(List list)
        {
            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
            INSERT INTO lists (title, cover_image)
            VALUES ($title, $coverImage);
            SELECT last_insert_rowid();
        ";

            cmd.Parameters.AddWithValue("$title", list.title);
            cmd.Parameters.AddWithValue("$coverImage", (object?)list.coverImage ?? DBNull.Value);

            var newId = (long)cmd.ExecuteScalar();
            return (int)newId;
        }

        // Update/Delete komen hier later ook
    }
}
