using Bookmark_App.CloudSync;
using Bookmark_App.Models;
using Microsoft.Data.Sqlite;
using System.Text;

namespace Bookmark_App.DataAccess
{
    public class ItemRepository
    {
        public List<ListItem> GetAllByList(List list, Genre genreFilter, string sort, string titleSearch, ItemStatus status, int itemsPerPage, int currentPage)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (currentPage <= 0) currentPage = 1; // treat pages as 1-based
            var result = new List<ListItem>();
            var itemsById = new Dictionary<int, ListItem>();

            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            // Determine ORDER BY clause (same logic as before)
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
                    orderBy = "i.progress_current ASC";
                    break;
                case "Progress Descending":
                    orderBy = "i.progress_current DESC";
                    break;
                default:
                    orderBy = "i.title COLLATE NOCASE ASC";
                    break;
            }

            // Build common WHERE filter fragment and parameters
            var whereSb = new StringBuilder();
            whereSb.AppendLine("WHERE i.list_id = $ListId");
            var commonParams = new Dictionary<string, object?>
            {
                ["$ListId"] = list.id
            };

            if (genreFilter != null && genreFilter.id != -1)
            {
                whereSb.AppendLine("AND g.id = $GenreId");
                commonParams["$GenreId"] = genreFilter.id;
            }

            if (!string.IsNullOrWhiteSpace(titleSearch))
            {
                whereSb.AppendLine("AND i.title LIKE $TitleSearch");
                commonParams["$TitleSearch"] = $"%{titleSearch}%";
            }

            if (status != ItemStatus.All)
            {
                whereSb.AppendLine("AND i.status = $Status");
                commonParams["$Status"] = (int)status;
            }

            List<int> pageItemIds;

            // If itemsPerPage is positive, first fetch the page of distinct item ids matching filters
            if (itemsPerPage > 0)
            {
                using var idCmd = connection.CreateCommand();
                var idSb = new StringBuilder();
                idSb.AppendLine(@"
                                SELECT DISTINCT i.id
                                FROM items i
                                LEFT JOIN item_genres ig ON ig.item_id = i.id
                                LEFT JOIN genres g       ON g.id = ig.genre_id
                                ");
                idSb.Append(whereSb.ToString());
                idSb.AppendLine($"ORDER BY {orderBy}");
                idSb.AppendLine("LIMIT $Limit OFFSET $Offset;");

                idCmd.CommandText = idSb.ToString();

                // add common params
                foreach (var kv in commonParams)
                    idCmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

                var limit = itemsPerPage;
                var offset = (long)(itemsPerPage * (currentPage - 1));
                idCmd.Parameters.AddWithValue("$Limit", limit);
                idCmd.Parameters.AddWithValue("$Offset", offset);

                using var reader = idCmd.ExecuteReader();
                pageItemIds = new List<int>();
                while (reader.Read())
                {
                    pageItemIds.Add(reader.GetInt32(0));
                }

                if (pageItemIds.Count == 0)
                    return result; // empty page
            }
            else
            {
                // No pagination requested: fetch all matching distinct item ids (preserves ordering)
                using var idCmd = connection.CreateCommand();
                var idSb = new StringBuilder();
                idSb.AppendLine(@"
SELECT DISTINCT i.id
FROM items i
LEFT JOIN item_genres ig ON ig.item_id = i.id
LEFT JOIN genres g       ON g.id = ig.genre_id
");
                idSb.Append(whereSb.ToString());
                idSb.AppendLine($"ORDER BY {orderBy};");

                idCmd.CommandText = idSb.ToString();

                foreach (var kv in commonParams)
                    idCmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);

                using var reader = idCmd.ExecuteReader();
                pageItemIds = new List<int>();
                while (reader.Read())
                {
                    pageItemIds.Add(reader.GetInt32(0));
                }

                if (pageItemIds.Count == 0)
                    return result;
            }

            // Now fetch full item rows (with genres) for the paged ids
            using var cmd = connection.CreateCommand();
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
");

            // Build IN clause with parameters for each id
            sb.Append("WHERE i.id IN (");
            for (int i = 0; i < pageItemIds.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"$id{i}");
            }
            sb.AppendLine(")");
            sb.AppendLine($"ORDER BY {orderBy}, ig.rowid ASC;");

            cmd.CommandText = sb.ToString();

            // Add id parameters
            for (int i = 0; i < pageItemIds.Count; i++)
                cmd.Parameters.AddWithValue($"$id{i}", pageItemIds[i]);

            using var reader2 = cmd.ExecuteReader();

            while (reader2.Read())
            {
                var itemId = reader2.GetInt32(0);

                if (!itemsById.TryGetValue(itemId, out var item))
                {
                    item = new ListItem
                    {
                        id = itemId,
                        title = reader2.IsDBNull(1) ? null : reader2.GetString(1),
                        status = (ItemStatus)reader2.GetInt32(2),
                        progressCurrent = reader2.IsDBNull(3) ? 0.0 : reader2.GetDouble(3),
                        progressMax = reader2.IsDBNull(4) ? 0.0 : reader2.GetDouble(4),
                        rating = reader2.IsDBNull(5) ? 0.0 : reader2.GetDouble(5),
                        url = reader2.IsDBNull(6) ? null : reader2.GetString(6),
                        coverImage = reader2.IsDBNull(7) ? null : (byte[])reader2["cover_image"],
                    };

                    itemsById[itemId] = item;
                    result.Add(item);
                }

                // Add genre if this row has one
                if (!reader2.IsDBNull(8))
                {
                    var genre = new Genre
                    {
                        id = reader2.GetInt32(8),
                        name = reader2.GetString(9)
                    };

                    item.genres.Add(genre);
                }
            }

            // The result list is ordered by the second query ORDER BY; however to
            // ensure the item order matches the pageItemIds order (in case of unusual ordering tied to genres),
            // we re-order result by pageItemIds to guarantee stable paging by item.
            var ordered = new List<ListItem>(result.Count);
            var map = new Dictionary<int, ListItem>(itemsById);
            foreach (var id in pageItemIds)
            {
                if (map.TryGetValue(id, out var it))
                    ordered.Add(it);
            }

            return ordered;
        }

        public int GetItemCount(List list, Genre genreFilter, string titleSearch, ItemStatus status)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();

            // Mirror the filters used in GetAllByList.
            var sb = new StringBuilder();
            sb.AppendLine(@"
                            SELECT COUNT(DISTINCT i.id)
                            FROM items i
                            LEFT JOIN item_genres ig ON ig.item_id = i.id
                            LEFT JOIN genres g       ON g.id = ig.genre_id
                            WHERE i.list_id = $ListId
                            ");

            cmd.Parameters.AddWithValue("$ListId", list.id);

            // Genre filter (ignore when genre.id == -1)
            if (genreFilter != null && genreFilter.id != -1)
            {
                sb.AppendLine("AND g.id = $GenreId");
                cmd.Parameters.AddWithValue("$GenreId", genreFilter.id);
            }

            // Title search (ignore when null/empty)
            if (!string.IsNullOrWhiteSpace(titleSearch))
            {
                sb.AppendLine("AND i.title LIKE $TitleSearch");
                cmd.Parameters.AddWithValue("$TitleSearch", $"%{titleSearch}%");
            }

            // Status filter (ignore when ItemStatus.All)
            if (status != ItemStatus.All)
            {
                sb.AppendLine("AND i.status = $Status");
                cmd.Parameters.AddWithValue("$Status", (int)status);
            }

            cmd.CommandText = sb.ToString();

            var result = cmd.ExecuteScalar();
            var count = result is long l ? l : Convert.ToInt64(result ?? 0L);
            return (int)count;
        }

        public void Update(ListItem listItem)
        {
            if (listItem == null) throw new ArgumentNullException(nameof(listItem));

            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();
            bool changed = false;
            using var transaction = connection.BeginTransaction();
            try
            {
                // 1) Remove all item_genres entries for this item
                using (var delCmd = connection.CreateCommand())
                {
                    delCmd.Transaction = transaction;
                    delCmd.CommandText = "DELETE FROM item_genres WHERE item_id = $itemId;";
                    delCmd.Parameters.AddWithValue("$itemId", listItem.id);
                    delCmd.ExecuteNonQuery();
                }

                // 2) Update the item row
                using (var updCmd = connection.CreateCommand())
                {
                    updCmd.Transaction = transaction;
                    updCmd.CommandText = @"
                                        UPDATE items
                                        SET title = $title,
                                            status = $status,
                                            progress_current = $progressCurrent,
                                            progress_max = $progressMax,
                                            rating = $rating,
                                            url = $url,
                                            cover_image = $coverImage
                                        WHERE id = $itemId;
                                        ";
                    updCmd.Parameters.AddWithValue("$title", (object?)listItem.title ?? DBNull.Value);
                    updCmd.Parameters.AddWithValue("$status", (int)listItem.status);
                    updCmd.Parameters.AddWithValue("$progressCurrent", listItem.progressCurrent);
                    // allow NULL for progress_max if the model uses a sentinel — store value directly
                    updCmd.Parameters.AddWithValue("$progressMax", listItem.progressMax);
                    updCmd.Parameters.AddWithValue("$rating", listItem.rating);
                    updCmd.Parameters.AddWithValue("$url", (object?)listItem.url ?? DBNull.Value);
                    updCmd.Parameters.AddWithValue("$coverImage", (object?)listItem.coverImage ?? DBNull.Value);
                    updCmd.Parameters.AddWithValue("$itemId", listItem.id);

                    updCmd.ExecuteNonQuery();
                    changed = true;
                }

                // 3) Insert new item_genres entries from listItem.genres (if any)
                if (listItem.genres != null && listItem.genres.Count > 0)
                {
                    using var insCmd = connection.CreateCommand();
                    insCmd.Transaction = transaction;
                    insCmd.CommandText = "INSERT INTO item_genres (item_id, genre_id) VALUES ($itemId, $genreId);";
                    var itemIdParam = insCmd.CreateParameter();
                    itemIdParam.ParameterName = "$itemId";
                    itemIdParam.Value = listItem.id;
                    insCmd.Parameters.Add(itemIdParam);

                    var genreIdParam = insCmd.CreateParameter();
                    genreIdParam.ParameterName = "$genreId";
                    insCmd.Parameters.Add(genreIdParam);

                    foreach (var g in listItem.genres)
                    {
                        // if genre object has no id, skip it
                        if (g == null || g.id == -1) continue;
                        genreIdParam.Value = g.id;
                        insCmd.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                SyncStateManager.Current.IsLocalDirty = true;
                SyncStateManager.Save();
                if (changed && SyncCoordinator.AutoSyncEnabled)
                {
                    SyncCoordinator.NotifyDbChanged?.Invoke();
                }
            }
            catch
            {
                try { transaction.Rollback(); } catch { /* ignore rollback errors */ }
                throw;
            }
        }

        public void Insert(ListItem listItem, int listId)
        {
            if (listItem == null) throw new ArgumentNullException(nameof(listItem));

            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            bool changed = false;

            using var transaction = connection.BeginTransaction();
            try
            {
                // Insert item and get new id
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
INSERT INTO items (list_id, title, status, progress_current, progress_max, rating, url, cover_image)
VALUES ($listId, $title, $status, $progressCurrent, $progressMax, $rating, $url, $coverImage);
SELECT last_insert_rowid();
";
                    cmd.Parameters.AddWithValue("$listId", listId);
                    cmd.Parameters.AddWithValue("$title", (object?)listItem.title ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$status", (int)listItem.status);
                    cmd.Parameters.AddWithValue("$progressCurrent", listItem.progressCurrent);
                    cmd.Parameters.AddWithValue("$progressMax", listItem.progressMax);
                    cmd.Parameters.AddWithValue("$rating", (object?)listItem.rating ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$url", (object?)listItem.url ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$coverImage", (object?)listItem.coverImage ?? DBNull.Value);

                    var newId = (long)cmd.ExecuteScalar();
                    listItem.id = (int)newId;
                    changed = true;
                }

                // Insert item_genres for the new item
                if (listItem.genres != null && listItem.genres.Count > 0)
                {
                    using var insCmd = connection.CreateCommand();
                    insCmd.Transaction = transaction;
                    insCmd.CommandText = "INSERT INTO item_genres (item_id, genre_id) VALUES ($itemId, $genreId);";
                    insCmd.Parameters.AddWithValue("$itemId", listItem.id);

                    var genreIdParam = insCmd.CreateParameter();
                    genreIdParam.ParameterName = "$genreId";
                    insCmd.Parameters.Add(genreIdParam);

                    foreach (var g in listItem.genres)
                    {
                        if (g == null) continue;
                        genreIdParam.Value = g.id;
                        insCmd.ExecuteNonQuery();
                    }
                }
                transaction.Commit();
                SyncStateManager.Current.IsLocalDirty = true;
                SyncStateManager.Save();
                if (changed && SyncCoordinator.AutoSyncEnabled)
                {
                    SyncCoordinator.NotifyDbChanged?.Invoke();
                }
            }
            catch
            {
                try { transaction.Rollback(); } catch { /* ignore rollback errors */ }
                throw;
            }
        }

        public void Delete(ListItem listItem)
        {
            if (listItem == null) throw new ArgumentNullException(nameof(listItem));

            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();
            bool changed = false;
            using var transaction = connection.BeginTransaction();
            try
            {
                // Remove item_genres (explicitly) then delete the item.
                using (var delGenres = connection.CreateCommand())
                {
                    delGenres.Transaction = transaction;
                    delGenres.CommandText = "DELETE FROM item_genres WHERE item_id = $itemId;";
                    delGenres.Parameters.AddWithValue("$itemId", listItem.id);
                    delGenres.ExecuteNonQuery();
                }

                using (var delItem = connection.CreateCommand())
                {
                    delItem.Transaction = transaction;
                    delItem.CommandText = "DELETE FROM items WHERE id = $itemId;";
                    delItem.Parameters.AddWithValue("$itemId", listItem.id);
                    delItem.ExecuteNonQuery();
                    changed = true;
                }

                transaction.Commit();
                SyncStateManager.Current.IsLocalDirty = true;
                SyncStateManager.Save();
                if (changed && SyncCoordinator.AutoSyncEnabled)
                {
                    SyncCoordinator.NotifyDbChanged?.Invoke();
                }
            }
            catch
            {
                try { transaction.Rollback(); } catch { /* ignore rollback errors */ }
                throw;
            }
        }

    }
}
