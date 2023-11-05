using CounterStrikeSharp.API;
using MySqlConnector;
using WeaponPaints;

namespace WeaponPaints
{
    internal class Database
    {
        private static readonly MySqlConnectionStringBuilder connection = new()
        {
            Server = Cfg.config.DatabaseHost,
            Port = Cfg.config.DatabasePort,
            UserID = Cfg.config.DatabaseUser,
            Password = Cfg.config.DatabasePassword,
            Database = Cfg.config.DatabaseName
        };

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connection.ConnectionString);
        }
    }
    internal class Queries
    {
        public static int GetPlayersWeaponPaint(string steamId, int weaponDefIndex)
        {
            try
            {
                using MySqlConnection connection = Database.GetConnection();
                using MySqlCommand command = connection.CreateCommand();
                command.CommandText = "SELECT weapon_paint_id FROM wp_player_skins WHERE steamid = @steamId AND weapon_defindex = @weaponDefIndex;";
                command.Parameters.AddWithValue("@steamId", steamId);
                command.Parameters.AddWithValue("@weaponDefIndex", weaponDefIndex);

                connection.Open();
                using var reader = command.ExecuteReader();

                int weaponPaint = 0;
                while (reader.Read())
                {
                    weaponPaint = reader.GetInt32("weapon_paint_id");
                }
                connection.Close();
                return weaponPaint;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}