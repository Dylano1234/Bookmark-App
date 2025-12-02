using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bookmark_App.DataAccess
{
    public static class DbConfig
    {
        public static string DatabasePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bookmark.db");

        public static string ConnectionString => $"Data Source={DatabasePath}";
    }
}
