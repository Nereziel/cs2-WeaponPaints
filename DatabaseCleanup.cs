using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using Dapper;

namespace WeaponPaints;

public class DatabaseCleanupService
{
    private readonly Database _database;
    private readonly WeaponPaintsConfig _config;
    private readonly ILogger _logger;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _cleanupTimer;

    public DatabaseCleanupService(Database database, WeaponPaintsConfig config, ILogger logger)
    {
        _database = database;
        _config = config;
        _logger = logger;
    }

    public void Initialize()
    {
        if (!_config.DatabaseCleanup.Enabled)
        {
            _logger.LogInformation("Database cleanup is disabled in configuration.");
            return;
        }

        if (_config.DatabaseCleanup.InactiveDays <= 0)
        {
            _logger.LogWarning("Invalid InactiveDays configuration. Database cleanup will not run.");
            return;
        }

        if (_config.DatabaseCleanup.RunOnStartup)
        {
            WeaponPaints.Instance.AddTimer(10.0f, () =>
            {
                _ = Task.Run(async () => await PerformCleanup());
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        float intervalSeconds = _config.DatabaseCleanup.CleanupIntervalMinutes * 60.0f;
        _cleanupTimer = WeaponPaints.Instance.AddTimer(intervalSeconds, () =>
        {
            _ = Task.Run(async () => await PerformCleanup());
        }, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);

        _logger.LogInformation($"Database cleanup service initialized. Will run every {_config.DatabaseCleanup.CleanupIntervalMinutes} minutes for users inactive more than {_config.DatabaseCleanup.InactiveDays} days.");
    }

    public async Task PerformCleanup()
    {
        try
        {
            await using var connection = await _database.GetConnectionAsync();

            var cutoffDate = DateTime.UtcNow.AddDays(-_config.DatabaseCleanup.InactiveDays);
            var cutoffDateString = cutoffDate.ToString("yyyy-MM-dd HH:mm:ss");

            int totalDeleted = 0;

            const string selectInactiveSteamIds = @"
				SELECT DISTINCT steamid 
				FROM wp_player_tracking 
				WHERE last_seen < @cutoffDate";

            var inactiveSteamIds = (await connection.QueryAsync<string>(
                selectInactiveSteamIds,
                new { cutoffDate = cutoffDateString }
            )).ToList();

            if (inactiveSteamIds.Count == 0)
            {
                if (_config.DatabaseCleanup.LogCleanup)
                {
                    _logger.LogInformation("Database cleanup: No inactive users found.");
                }
                return;
            }

            if (_config.Additional.SkinEnabled)
            {
                const string deleteSkins = "DELETE FROM `wp_player_skins` WHERE `steamid` IN @steamids";
                var deleted = await connection.ExecuteAsync(deleteSkins, new { steamids = inactiveSteamIds });
                totalDeleted += deleted;
                if (_config.DatabaseCleanup.LogCleanup)
                {
                    _logger.LogInformation($"Deleted {deleted} skin records for inactive users.");
                }
            }

            if (_config.Additional.KnifeEnabled)
            {
                const string deleteKnives = "DELETE FROM `wp_player_knife` WHERE `steamid` IN @steamids";
                var deleted = await connection.ExecuteAsync(deleteKnives, new { steamids = inactiveSteamIds });
                totalDeleted += deleted;
                if (_config.DatabaseCleanup.LogCleanup)
                {
                    _logger.LogInformation($"Deleted {deleted} knife records for inactive users.");
                }
            }

            if (_config.Additional.GloveEnabled)
            {
                const string deleteGloves = "DELETE FROM `wp_player_gloves` WHERE `steamid` IN @steamids";
                var deleted = await connection.ExecuteAsync(deleteGloves, new { steamids = inactiveSteamIds });
                totalDeleted += deleted;
                if (_config.DatabaseCleanup.LogCleanup)
                {
                    _logger.LogInformation($"Deleted {deleted} glove records for inactive users.");
                }
            }

            if (_config.Additional.AgentEnabled)
            {
                const string deleteAgents = "DELETE FROM `wp_player_agents` WHERE `steamid` IN @steamids";
                var deleted = await connection.ExecuteAsync(deleteAgents, new { steamids = inactiveSteamIds });
                totalDeleted += deleted;
                if (_config.DatabaseCleanup.LogCleanup)
                {
                    _logger.LogInformation($"Deleted {deleted} agent records for inactive users.");
                }
            }

            if (_config.Additional.MusicEnabled)
            {
                const string deleteMusic = "DELETE FROM `wp_player_music` WHERE `steamid` IN @steamids";
                var deleted = await connection.ExecuteAsync(deleteMusic, new { steamids = inactiveSteamIds });
                totalDeleted += deleted;
                if (_config.DatabaseCleanup.LogCleanup)
                {
                    _logger.LogInformation($"Deleted {deleted} music records for inactive users.");
                }
            }

            if (_config.Additional.PinsEnabled)
            {
                const string deletePins = "DELETE FROM `wp_player_pins` WHERE `steamid` IN @steamids";
                var deleted = await connection.ExecuteAsync(deletePins, new { steamids = inactiveSteamIds });
                totalDeleted += deleted;
                if (_config.DatabaseCleanup.LogCleanup)
                {
                    _logger.LogInformation($"Deleted {deleted} pin records for inactive users.");
                }
            }

            const string deleteTracking = "DELETE FROM `wp_player_tracking` WHERE `steamid` IN @steamids";
            await connection.ExecuteAsync(deleteTracking, new { steamids = inactiveSteamIds });

            if (_config.DatabaseCleanup.LogCleanup)
            {
                _logger.LogInformation($"Database cleanup completed: {totalDeleted} total records deleted for {inactiveSteamIds.Count} inactive users.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during database cleanup: {ex.Message}");
        }
    }

    public async Task UpdatePlayerActivity(string steamId)
    {
        if (!_config.DatabaseCleanup.Enabled || string.IsNullOrEmpty(steamId))
            return;

        try
        {
            await using var connection = await _database.GetConnectionAsync();

            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            const string query = @"
				INSERT INTO `wp_player_tracking` (`steamid`, `last_seen`, `first_seen`)
				VALUES (@steamid, @now, @now)
				ON DUPLICATE KEY UPDATE `last_seen` = @now";

            await connection.ExecuteAsync(query, new { steamid = steamId, now });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating player activity tracking: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Kill();
    }
}