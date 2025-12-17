using Bookmark_App.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bookmark_App.DataAccess
{
    public class ItemRepository
    {
        public List<ListItem> GetAllByList(List list, Genre genreFilter, string sort, string titleSearch, ItemStatus status)
        {
            var result = new List<ListItem>();
            var itemsById = new Dictionary<int, ListItem>();

            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();

            // Build base query
            var sb = new StringBuilder();
            sb.AppendLine(@"
        SELECT 
            i.id,
            i.title,
            i.status,
            i.progress_current,
            i.progress_max,
            i.rating,
            i.url,
            i.cover_image,
            g.id   AS genre_id,
            g.name AS genre_name
        FROM items i
        LEFT JOIN item_genres ig ON ig.item_id = i.id
        LEFT JOIN genres g       ON g.id = ig.genre_id
        WHERE i.list_id = @ListId
        ");

            cmd.Parameters.AddWithValue("@ListId", list.id);

            // Genre filter (ignore when genre.id == -1)
            if (genreFilter != null && genreFilter.id != -1)
            {
                sb.AppendLine("AND g.id = @GenreId");
                cmd.Parameters.AddWithValue("@GenreId", genreFilter.id);
            }

            // Title search (ignore when null/empty)
            if (!string.IsNullOrWhiteSpace(titleSearch))
            {
                sb.AppendLine("AND i.title LIKE @TitleSearch");
                cmd.Parameters.AddWithValue("@TitleSearch", $"%{titleSearch}%");
            }

            // Status filter (ignore when ItemStatus.All)
            if (status != ItemStatus.All)
            {
                sb.AppendLine("AND i.status = @Status");
                cmd.Parameters.AddWithValue("@Status", (int)status);
            }

            // Sorting
            // Map known sorting strings to ORDER BY clauses; fall back to title
            string orderBy;
            switch ((sort ?? "").Trim())
            {
                case "Title Ascending":
                    orderBy = "i.title COLLATE NOCASE ASC";
                    break;
                case "Title Descending":
                    orderBy = "i.title COLLATE NOCASE DESC";
                    break;
                case "Rating Ascending":
                    orderBy = "i.rating ASC";
                    break;
                case "Rating Descending":
                    orderBy = "i.rating DESC";
                    break;
                case "Progress Ascending":
                    // Sort by progress percentage; guard division by zero with NULLIF
                    orderBy = "CAST(i.progress_current AS REAL) / NULLIF(i.progress_max,0) ASC";
                    break;
                case "Progress Descending":
                    orderBy = "CAST(i.progress_current AS REAL) / NULLIF(i.progress_max,0) DESC";
                    break;
                default:
                    orderBy = "i.title COLLATE NOCASE ASC";
                    break;
            }

            sb.AppendLine($"ORDER BY {orderBy}, g.name;");

            cmd.CommandText = sb.ToString();

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var itemId = reader.GetInt32(0);

                // Create the ListItem once per id
                if (!itemsById.TryGetValue(itemId, out var item))
                {
                    item = new ListItem
                    {
                        id = itemId,
                        title = reader.IsDBNull(1) ? null : reader.GetString(1),
                        status = (ItemStatus)reader.GetInt32(2),
                        progressCurrent = reader.IsDBNull(3) ? 0.0 : reader.GetDouble(3),
                        progressMax = reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4),
                        rating = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5),
                        url = reader.IsDBNull(6) ? null : reader.GetString(6),
                        coverImage = reader.IsDBNull(7) ? null : (byte[])reader["cover_image"],
                    };

                    itemsById[itemId] = item;
                    result.Add(item);
                }

                // Add genre if this row has one
                if (!reader.IsDBNull(8))
                {
                    var genre = new Genre
                    {
                        id = reader.GetInt32(8),
                        name = reader.GetString(9)
                    };

                    item.genres.Add(genre);
                }
            }

            return result;
        }
    }
}
