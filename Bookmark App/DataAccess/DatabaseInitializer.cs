using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Bookmark_App.DataAccess
{
    public static class DatabaseInitializer
    {
        public static void Initialize()
        {
            var dbPath = DbConfig.DatabasePath;
            bool newDatabase = !File.Exists(dbPath);

            using var connection = new SqliteConnection(DbConfig.ConnectionString);
            connection.Open();

            CreateTables(connection);

            if (newDatabase)
            {
                SeedGenres(connection);
            }
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
            // simpele check: als er al genres zijn, niks doen
            using (var checkCmd = connection.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM genres;";
                long count = (long)checkCmd.ExecuteScalar();
                if (count > 0)
                    return;
            }

            string[] defaultGenres =
            {
            "Action", "Adventure", "Comedy", "Drama", "Fantasy",
            "Sci-Fi", "Slice of Life", "Romance", "Horror",
            "Mystery", "Thriller", "Sports", "Isekai"
        };

            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO genres (name) VALUES ($name);";
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
