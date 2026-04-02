using Microsoft.Data.Sqlite;
using System.IO;

namespace Bookmark_App.DataAccess
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            CreateTables(connection);
            SeedGenres(connection);
        }

        private static void CreateTables(SqliteConnection connection)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS lists (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    title       TEXT NOT NULL,
    cover_image BLOB
);

CREATE TABLE IF NOT EXISTS items (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    list_id          INTEGER NOT NULL,
    title            TEXT NOT NULL,
    status           INTEGER NOT NULL,          -- enum ItemStatus
    progress_current DECIMAL(5,1) NOT NULL DEFAULT 0.0,
    progress_max     DECIMAL(5,1),
    rating           DECIMAL(3,1),
    url              TEXT,
    cover_image      BLOB,
    FOREIGN KEY (list_id) REFERENCES lists(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS genres (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS item_genres (
    item_id  INTEGER NOT NULL,
    genre_id INTEGER NOT NULL,
    PRIMARY KEY (item_id, genre_id),
    FOREIGN KEY (item_id) REFERENCES items(id) ON DELETE CASCADE,
    FOREIGN KEY (genre_id) REFERENCES genres(id) ON DELETE CASCADE
);
";
            cmd.ExecuteNonQuery();
        }

        private static void SeedGenres(SqliteConnection connection)
        {
            string[] defaultGenres =
            {
                "Action", "Adventure", "Comedy", "Drama", "Fantasy",
                "Sci-Fi", "Slice of Life", "Romance", "Horror",
                "Mystery", "Thriller", "Sports", "Isekai", "Adult",
                "Martial Arts", "Supernatural", "Historical", "Mecha", "Music",
                "Reincarnation", "School", "Superhuman", "Psychological", 
                "Regression", "Time Travel", "System", "Dungeon",
                "Seinen", "Shounen", "Shoujo", "Josei", "Villainess"
            };

            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT OR IGNORE INTO genres (name) VALUES ($name);";
            var nameParam = insertCmd.CreateParameter();
            nameParam.ParameterName = "$name";
            insertCmd.Parameters.Add(nameParam);

            foreach (var g in defaultGenres)
            {
                nameParam.Value = g;
                insertCmd.ExecuteNonQuery();
            }
        }
    }
}
