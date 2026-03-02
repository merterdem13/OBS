using Microsoft.Data.Sqlite;

namespace AlparslanOBS.DataAccess
{
    /// <summary>
    /// Sistem ayarlarını (PIN vb.) veritabanındaki Settings tablosunda tutan katman.
    /// </summary>
    public class SettingsRepository
    {
        public SettingsRepository()
        {
            DatabaseConnection.EnsureDatabase();
        }

        public string? GetSetting(string key)
        {
            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @k LIMIT 1;";
            cmd.Parameters.AddWithValue("@k", key);

            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        public void SetSetting(string key, string value)
        {
            using var conn = DatabaseConnection.GetConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            // PIN gibi ayarları UPSERT mantığıyla ekler veya günceller
            cmd.CommandText = @"
                INSERT INTO Settings (Key, Value) VALUES (@k, @v)
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
            cmd.Parameters.AddWithValue("@k", key);
            cmd.Parameters.AddWithValue("@v", value);
            cmd.ExecuteNonQuery();
        }
    }
}