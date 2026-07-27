using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace WeaponPaints;

public partial class WeaponPaints
{
    private sealed class WeaponTarget
    {
        public string GiveName { get; set; } = "";
        public int DefIndex { get; set; }
        public int Slot { get; set; } // 1 = primary, 2 = secondary
    }

    private sealed class ParsedInspectPreview
    {
        public int DefIndex { get; set; }
        public int Paint { get; set; }
        public int Seed { get; set; }
        public float Wear { get; set; }

        public bool StatTrak { get; set; }
        public int StatTrakCount { get; set; }

        public List<StickerInfo> Stickers { get; set; } = new();
        public KeyChainInfo? KeyChain { get; set; }

        public ParsedInspectPreview()
        {
            for (int i = 0; i < 5; i++)
            {
                Stickers.Add(new StickerInfo
                {
                    Id = 0,
                    Schema = 0,
                    Wear = 0.0f,
                    Scale = 1.0f,
                    OffsetX = 0.0f,
                    OffsetY = 0.0f,
                    Rotation = 0.0f
                });
            }
        }
    }

    private static readonly Dictionary<int, WeaponTarget> WeaponsByDefIndex = new()
    {
        [7]  = new() { GiveName = "weapon_ak47", DefIndex = 7, Slot = 1 },
        [16] = new() { GiveName = "weapon_m4a1", DefIndex = 16, Slot = 1 },
        [60] = new() { GiveName = "weapon_m4a1_silencer", DefIndex = 60, Slot = 1 },
        [9]  = new() { GiveName = "weapon_awp", DefIndex = 9, Slot = 1 },
        [40] = new() { GiveName = "weapon_ssg08", DefIndex = 40, Slot = 1 },
        [8]  = new() { GiveName = "weapon_aug", DefIndex = 8, Slot = 1 },
        [39] = new() { GiveName = "weapon_sg556", DefIndex = 39, Slot = 1 },
        [10] = new() { GiveName = "weapon_famas", DefIndex = 10, Slot = 1 },
        [13] = new() { GiveName = "weapon_galilar", DefIndex = 13, Slot = 1 },
        [38] = new() { GiveName = "weapon_scar20", DefIndex = 38, Slot = 1 },
        [11] = new() { GiveName = "weapon_g3sg1", DefIndex = 11, Slot = 1 },
        [34] = new() { GiveName = "weapon_mp9", DefIndex = 34, Slot = 1 },
        [17] = new() { GiveName = "weapon_mac10", DefIndex = 17, Slot = 1 },
        [24] = new() { GiveName = "weapon_ump45", DefIndex = 24, Slot = 1 },
        [19] = new() { GiveName = "weapon_p90", DefIndex = 19, Slot = 1 },
        [26] = new() { GiveName = "weapon_bizon", DefIndex = 26, Slot = 1 },
        [33] = new() { GiveName = "weapon_mp7", DefIndex = 33, Slot = 1 },
        [23] = new() { GiveName = "weapon_mp5sd", DefIndex = 23, Slot = 1 },
        [35] = new() { GiveName = "weapon_nova", DefIndex = 35, Slot = 1 },
        [25] = new() { GiveName = "weapon_xm1014", DefIndex = 25, Slot = 1 },
        [27] = new() { GiveName = "weapon_mag7", DefIndex = 27, Slot = 1 },
        [29] = new() { GiveName = "weapon_sawedoff", DefIndex = 29, Slot = 1 },
        [14] = new() { GiveName = "weapon_m249", DefIndex = 14, Slot = 1 },
        [28] = new() { GiveName = "weapon_negev", DefIndex = 28, Slot = 1 },

        [1]  = new() { GiveName = "weapon_deagle", DefIndex = 1, Slot = 2 },
        [64] = new() { GiveName = "weapon_revolver", DefIndex = 64, Slot = 2 },
        [4]  = new() { GiveName = "weapon_glock", DefIndex = 4, Slot = 2 },
        [61] = new() { GiveName = "weapon_usp_silencer", DefIndex = 61, Slot = 2 },
        [32] = new() { GiveName = "weapon_hkp2000", DefIndex = 32, Slot = 2 },
        [36] = new() { GiveName = "weapon_p250", DefIndex = 36, Slot = 2 },
        [3]  = new() { GiveName = "weapon_fiveseven", DefIndex = 3, Slot = 2 },
        [63] = new() { GiveName = "weapon_cz75a", DefIndex = 63, Slot = 2 },
        [30] = new() { GiveName = "weapon_tec9", DefIndex = 30, Slot = 2 },
        // Knives (slot 0) — defindex mapping from Variables.cs
        [500] = new() { GiveName = "weapon_bayonet",              DefIndex = 500, Slot = 0 },
        [503] = new() { GiveName = "weapon_knife_css",            DefIndex = 503, Slot = 0 },
        [505] = new() { GiveName = "weapon_knife_flip",           DefIndex = 505, Slot = 0 },
        [506] = new() { GiveName = "weapon_knife_gut",            DefIndex = 506, Slot = 0 },
        [507] = new() { GiveName = "weapon_knife_karambit",       DefIndex = 507, Slot = 0 },
        [508] = new() { GiveName = "weapon_knife_m9_bayonet",     DefIndex = 508, Slot = 0 },
        [509] = new() { GiveName = "weapon_knife_tactical",       DefIndex = 509, Slot = 0 },
        [512] = new() { GiveName = "weapon_knife_falchion",       DefIndex = 512, Slot = 0 },
        [514] = new() { GiveName = "weapon_knife_survival_bowie", DefIndex = 514, Slot = 0 },
        [515] = new() { GiveName = "weapon_knife_butterfly",      DefIndex = 515, Slot = 0 },
        [516] = new() { GiveName = "weapon_knife_push",           DefIndex = 516, Slot = 0 },
        [517] = new() { GiveName = "weapon_knife_cord",           DefIndex = 517, Slot = 0 },
        [518] = new() { GiveName = "weapon_knife_canis",          DefIndex = 518, Slot = 0 },
        [519] = new() { GiveName = "weapon_knife_ursus",          DefIndex = 519, Slot = 0 },
        [520] = new() { GiveName = "weapon_knife_gypsy_jackknife", DefIndex = 520, Slot = 0 },
        [521] = new() { GiveName = "weapon_knife_outdoor",        DefIndex = 521, Slot = 0 },
        [522] = new() { GiveName = "weapon_knife_stiletto",       DefIndex = 522, Slot = 0 },
        [523] = new() { GiveName = "weapon_knife_widowmaker",     DefIndex = 523, Slot = 0 },
        [525] = new() { GiveName = "weapon_knife_skeleton",       DefIndex = 525, Slot = 0 },
        [526] = new() { GiveName = "weapon_knife_kukri",          DefIndex = 526, Slot = 0 },

        // Gloves (slot -1 = special handling)
        [5027] = new() { GiveName = "weapon_gg_gloves_sport_full",      DefIndex = 5027, Slot = -1 },
        [5028] = new() { GiveName = "weapon_gg_gloves_slick",           DefIndex = 5028, Slot = -1 },
        [5029] = new() { GiveName = "weapon_gg_gloves_handwrap_leathery", DefIndex = 5029, Slot = -1 },
        [5030] = new() { GiveName = "weapon_gg_gloves_motorcycle",      DefIndex = 5030, Slot = -1 },
        [5031] = new() { GiveName = "weapon_gg_gloves_specialist",      DefIndex = 5031, Slot = -1 },
        [5032] = new() { GiveName = "weapon_gg_gloves_snake_wrap",      DefIndex = 5032, Slot = -1 },
        [4725] = new() { GiveName = "weapon_gg_gloves_bloodhound",      DefIndex = 4725, Slot = -1 },
        [4770] = new() { GiveName = "weapon_gg_gloves_broken_fang",     DefIndex = 4770, Slot = -1 },
    };

    private static readonly HashSet<string> PrimaryWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_ak47",
        "weapon_m4a1",
        "weapon_m4a1_silencer",
        "weapon_awp",
        "weapon_ssg08",
        "weapon_aug",
        "weapon_sg556",
        "weapon_famas",
        "weapon_galilar",
        "weapon_scar20",
        "weapon_g3sg1",
        "weapon_mp9",
        "weapon_mac10",
        "weapon_ump45",
        "weapon_p90",
        "weapon_bizon",
        "weapon_mp7",
        "weapon_mp5sd",
        "weapon_nova",
        "weapon_xm1014",
        "weapon_mag7",
        "weapon_sawedoff",
        "weapon_m249",
        "weapon_negev"
    };

    private static readonly HashSet<string> SecondaryWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        "weapon_deagle",
        "weapon_revolver",
        "weapon_glock",
        "weapon_usp_silencer",
        "weapon_hkp2000",
        "weapon_p250",
        "weapon_fiveseven",
        "weapon_cz75a",
        "weapon_tec9",
        "weapon_elite"
    };

    [ConsoleCommand("i", "Give weapon from csgo_econ_action_preview console link")]
    public void OnConsoleLinkGive(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !Utility.IsPlayerValid(player))
            return;

        if (player.PlayerPawn.Value == null || !player.PawnIsAlive)
        {
            player.PrintToChat(" \x04[!i]\x01 You must be alive to use this command.");
            return;
        }

        var args = new List<string>();

        for (int i = 1; i < command.ArgCount; i++)
        {
            var arg = command.GetArg(i);

            if (!string.IsNullOrWhiteSpace(arg))
                args.Add(arg.Trim());
        }

        if (args.Count == 0)
        {
            player.PrintToChat(" \x04[!i]\x01 Usage:");
            player.PrintToChat(" \x10!i csgo_econ_action_preview <hex>");
            player.PrintToChat(" \x10!i <hex>");
            return;
        }

        string previewHex = ExtractPreviewHex(args);

        if (string.IsNullOrWhiteSpace(previewHex))
        {
            player.PrintToChat(" \x04[!i]\x01 Could not extract inspect hex.");
            return;
        }

        if (!TryParseInspectPreview(previewHex, out var preview, out string error))
        {
            player.PrintToChat($" \x04[!i]\x01 Parse failed: \x10{error}");
            return;
        }

        if (!WeaponsByDefIndex.TryGetValue(preview.DefIndex, out var targetWeapon))
        {
            player.PrintToChat($" \x04[!i]\x01 Unsupported weapon defindex: \x10{preview.DefIndex}");
            return;
        }

        SaveWeaponInfoForPlayer(player, targetWeapon, preview);

        // ── Glove special path ──────────────────────────────────────────────
        if (targetWeapon.Slot == -1)
        {
            var teamsToApply = player.TeamNum < 2
                ? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
                : new[] { player.Team };

            var playerGloves = GPlayersGlove.GetOrAdd(player.Slot,
                _ => new System.Collections.Concurrent.ConcurrentDictionary<CsTeam, ushort>());

            foreach (var team in teamsToApply)
                playerGloves[team] = (ushort)targetWeapon.DefIndex;

            AddTimer(0.10f, () => GivePlayerGloves(player));

            player.PrintToChat(
                $" \x04[!i]\x01 Given glove \x10{targetWeapon.GiveName}\x01 | paint=\x10{preview.Paint}\x01 seed=\x10{preview.Seed}\x01 wear=\x10{preview.Wear.ToString(CultureInfo.InvariantCulture)}\x01"
            );
            SyncPlayerToDatabase(player);
            return;
        }

        // ── Knife special path ──────────────────────────────────────────────
        if (targetWeapon.Slot == 0)
        {
            var teamsToApply = player.TeamNum < 2
                ? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
                : new[] { player.Team };

            var playerKnives = GPlayersKnife.GetOrAdd(player.Slot,
                _ => new System.Collections.Concurrent.ConcurrentDictionary<CsTeam, string>());

            foreach (var team in teamsToApply)
                playerKnives[team] = targetWeapon.GiveName;

            AddTimer(0.10f, () =>
            {
                if (player != null && Utility.IsPlayerValid(player) && player.PawnIsAlive)
                    RefreshWeapons(player);
            });

            player.PrintToChat(
                $" \x04[!i]\x01 Given knife \x10{targetWeapon.GiveName}\x01 | paint=\x10{preview.Paint}\x01 seed=\x10{preview.Seed}\x01 wear=\x10{preview.Wear.ToString(CultureInfo.InvariantCulture)}\x01"
            );
            SyncPlayerToDatabase(player);
            return;
        }

        // ── Normal weapon give ───────────────────────────────────────
        RemoveWeaponsInSameSlot(player, targetWeapon.Slot);

        player.GiveNamedItem(targetWeapon.GiveName);
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");

        AddTimer(0.20f, () =>
        {
            if (player == null || !Utility.IsPlayerValid(player) || !player.PawnIsAlive)
                return;

            RefreshWeapons(player);
        });

        SyncPlayerToDatabase(player);

        // Also write all 5 sticker slots to DB so old stickers are cleared when new link has fewer
        if (WeaponSync != null)
        {
            var playerInfo = new PlayerInfo
            {
                UserId    = player.UserId,
                Slot      = player.Slot,
                Index     = (int)player.Index,
                SteamId   = player.SteamID.ToString(),
                Name      = player.PlayerName,
                IpAddress = player.IpAddress?.Split(":")[0]
            };
            _ = Task.Run(async () => await WeaponSync.SyncStickersToDatabase(playerInfo, targetWeapon.DefIndex, preview.Stickers));
        }

        int stickerCount = preview.Stickers.Count(x => x.Id != 0);

        string charmText = preview.KeyChain != null
            ? $" | charm={preview.KeyChain.Id}"
            : "";

        string stText = preview.StatTrak
            ? $" | StatTrak={preview.StatTrakCount}"
            : "";

        player.PrintToChat(
            $" \x04[!i]\x01 Given \x10{targetWeapon.GiveName}\x01 | paint=\x10{preview.Paint}\x01 seed=\x10{preview.Seed}\x01 wear=\x10{preview.Wear.ToString(CultureInfo.InvariantCulture)}\x01 | stickers=\x10{stickerCount}\x01{charmText}{stText}"
        );
    }

    private static string ExtractPreviewHex(List<string> args)
    {
        string joined = string.Join(" ", args).Trim().Trim('"');

        joined = joined.Replace("%20", " ");
        joined = joined.Replace("+", " ");

        int previewIndex = joined.IndexOf("csgo_econ_action_preview", StringComparison.OrdinalIgnoreCase);

        if (previewIndex >= 0)
        {
            joined = joined.Substring(previewIndex + "csgo_econ_action_preview".Length).Trim();
        }

        joined = Regex.Replace(joined, @"[^0-9a-fA-F]", "");

        return joined;
    }

    private static bool TryParseInspectPreview(string hex, out ParsedInspectPreview preview, out string error)
    {
        preview = new ParsedInspectPreview();
        error = "";

        try
        {
            if (hex.Length < 4)
            {
                error = "Inspect hex too short.";
                return false;
            }

            if (hex.Length % 2 != 0)
                hex = hex[..^1];

            byte[] rawData = Convert.FromHexString(hex);
            byte[] data = AlignToPreviewStart(rawData);

            int index = 0;

            bool sawStatTrakField = false;
            int statTrakType = 0;

            while (index < data.Length)
            {
                if (!TryReadVarint(data, ref index, out ulong key))
                    break;

                int field = (int)(key >> 3);
                int wireType = (int)(key & 0x07);

                if (field <= 0 || !IsSupportedWireType(wireType))
                    break;

                switch (field)
                {
                    case 3:
                        if (!TryReadVarint(data, ref index, out ulong defIndex))
                            index = data.Length;
                        else
                            preview.DefIndex = (int)defIndex;
                        break;

                    case 4:
                        if (!TryReadVarint(data, ref index, out ulong paint))
                            index = data.Length;
                        else
                            preview.Paint = (int)paint;
                        break;

                    case 7:
                    {
                        if (wireType == 2)
                        {
                            if (!TrySkipField(data, ref index, wireType))
                                index = data.Length;
                            break;
                        }

                        if (!TryReadVarint(data, ref index, out ulong rawWearValue))
                        {
                            index = data.Length;
                            break;
                        }

                        uint rawWear = (uint)rawWearValue;
                        preview.Wear = BitConverter.Int32BitsToSingle((int)rawWear);
                        break;
                    }

                    case 8:
                        if (!TryReadVarint(data, ref index, out ulong seed))
                            index = data.Length;
                        else
                            preview.Seed = (int)seed;
                        break;

                    case 9:
                        if (!TryReadVarint(data, ref index, out ulong stType))
                            index = data.Length;
                        else
                        {
                            statTrakType = (int)stType;
                            sawStatTrakField = true;
                        }
                        break;

                    case 10:
                    {
                        if (!TryReadVarint(data, ref index, out ulong value))
                        {
                            index = data.Length;
                            break;
                        }

                        int intValue = (int)value;

                        if (!sawStatTrakField)
                        {
                            statTrakType = intValue;
                            sawStatTrakField = true;
                        }
                        else
                        {
                            preview.StatTrak = true;
                            preview.StatTrakCount = intValue;
                        }

                        break;
                    }

                    case 11:
                        if (!TryReadVarint(data, ref index, out ulong stCount))
                            index = data.Length;
                        else
                        {
                            preview.StatTrak = true;
                            preview.StatTrakCount = (int)stCount;
                        }
                        break;

                    case 12:
                    {
                        if (!TryReadLengthDelimited(data, ref index, out byte[] stickerBytes))
                        {
                            index = data.Length;
                            break;
                        }

                        ParseStickerMessage(stickerBytes, preview);
                        break;
                    }

                    case 20:
                    {
                        if (!TryReadLengthDelimited(data, ref index, out byte[] keychainBytes))
                        {
                            index = data.Length;
                            break;
                        }

                        ParseKeychainMessage(keychainBytes, preview);
                        break;
                    }

                    default:
                        if (!TrySkipField(data, ref index, wireType))
                            index = data.Length;
                        break;
                }
            }

            if (sawStatTrakField && statTrakType > 0)
                preview.StatTrak = true;

            if (preview.DefIndex == 0)
            {
                string firstBytes = BitConverter.ToString(data.Take(16).ToArray());
                error = $"Missing defindex. First bytes: {firstBytes}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static byte[] AlignToPreviewStart(byte[] rawData)
    {
        if (rawData.Length == 0)
            return rawData;

        int maxScan = Math.Min(rawData.Length, 16);

        for (int start = 0; start < maxScan; start++)
        {
            try
            {
                int index = start;

                if (!TryReadVarint(rawData, ref index, out ulong key))
                    continue;

                int field = (int)(key >> 3);
                int wireType = (int)(key & 0x07);

                if (field != 3 || wireType != 0)
                    continue;

                int tempIndex = index;

                if (!TryReadVarint(rawData, ref tempIndex, out ulong possibleDefIndex))
                    continue;

                if (WeaponsByDefIndex.ContainsKey((int)possibleDefIndex))
                    return rawData.Skip(start).ToArray();
            }
            catch
            {
                // ignore
            }
        }

        if (rawData[0] == 0x00)
            return rawData.Skip(1).ToArray();

        return rawData;
    }

    private static void ParseStickerMessage(byte[] data, ParsedInspectPreview preview)
    {
        int index = 0;

        int parsedSlot = -1;
        uint stickerId = 0;

        float wear = 0.0f;
        float scale = 1.0f;
        float rotation = 0.0f;
        float offsetX = 0.0f;
        float offsetY = 0.0f;

        while (index < data.Length)
        {
            if (!TryReadVarint(data, ref index, out ulong key))
                break;

            int field = (int)(key >> 3);
            int wireType = (int)(key & 0x07);

            if (field <= 0 || !IsSupportedWireType(wireType))
                break;

            switch (field)
            {
                case 1: // slot
                    if (!TryReadVarint(data, ref index, out ulong slotValue))
                        index = data.Length;
                    else
                        parsedSlot = (int)slotValue;
                    break;

                case 2: // sticker id
                    if (!TryReadVarint(data, ref index, out ulong idValue))
                        index = data.Length;
                    else
                        stickerId = (uint)idValue;
                    break;

                case 3: // wear / scratch
                    if (!TryReadFixed32Float(data, ref index, out wear))
                        index = data.Length;
                    break;

                case 4: // scale
                    if (!TryReadFixed32Float(data, ref index, out scale))
                        index = data.Length;
                    break;

                case 5: // rotation
                    if (!TryReadFixed32Float(data, ref index, out rotation))
                        index = data.Length;
                    break;

                case 6: // tint_id (varint, skip)
                    if (!TryReadVarint(data, ref index, out _))
                        index = data.Length;
                    break;

                case 7: // offset_x
                    if (!TryReadFixed32Float(data, ref index, out offsetX))
                        index = data.Length;
                    break;

                case 8: // offset_y
                    if (!TryReadFixed32Float(data, ref index, out offsetY))
                        index = data.Length;
                    break;

                default:
                    if (!TrySkipField(data, ref index, wireType))
                        index = data.Length;
                    break;
            }
        }

        if (stickerId == 0)
            return;

        int slot = parsedSlot;

        if (slot < 0 || slot >= 5 || preview.Stickers[slot].Id != 0)
        {
            slot = FindFirstEmptyStickerSlot(preview);
        }

        if (slot < 0 || slot >= 5)
            return;

        preview.Stickers[slot] = new StickerInfo
        {
            Id = stickerId,
            Schema = 0,
            Wear = Math.Clamp(wear, 0.0f, 1.0f),
            Scale = scale,
            Rotation = rotation,
            OffsetX = offsetX,
            OffsetY = offsetY
        };
    }

    private static int FindFirstEmptyStickerSlot(ParsedInspectPreview preview)
    {
        for (int i = 0; i < preview.Stickers.Count && i < 5; i++)
        {
            if (preview.Stickers[i].Id == 0)
                return i;
        }

        return -1;
    }

    private static void ParseKeychainMessage(byte[] data, ParsedInspectPreview preview)
    {
        int index = 0;

        uint id = 0;
        uint seed = 0;

        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;

        while (index < data.Length)
        {
            if (!TryReadVarint(data, ref index, out ulong key))
                break;

            int field = (int)(key >> 3);
            int wireType = (int)(key & 0x07);

            if (field <= 0 || !IsSupportedWireType(wireType))
                break;

            switch (field)
            {
                case 1: // slot
                    _ = TryReadVarint(data, ref index, out _);
                    break;

                case 2: // keychain/charm id
                    if (!TryReadVarint(data, ref index, out ulong idValue))
                        index = data.Length;
                    else
                        id = (uint)idValue;
                    break;

                case 7: // offset_x
                    if (!TryReadFixed32Float(data, ref index, out x))
                        index = data.Length;
                    break;

                case 8: // offset_y
                    if (!TryReadFixed32Float(data, ref index, out y))
                        index = data.Length;
                    break;

                case 9: // offset_z
                    if (!TryReadFixed32Float(data, ref index, out z))
                        index = data.Length;
                    break;

                case 10: // pattern / keychain seed
                    if (!TryReadVarint(data, ref index, out ulong seedValue))
                        index = data.Length;
                    else
                        seed = (uint)seedValue;
                    break;

                default:
                    if (!TrySkipField(data, ref index, wireType))
                        index = data.Length;
                    break;
            }
        }

        if (id == 0)
            return;

        preview.KeyChain = new KeyChainInfo
        {
            Id = id,
            Seed = seed,
            OffsetX = x,
            OffsetY = y,
            OffsetZ = z
        };
    }

    private static bool TryReadVarint(byte[] data, ref int index, out ulong value)
    {
        value = 0;
        int shift = 0;

        while (index < data.Length)
        {
            byte b = data[index++];

            value |= ((ulong)(b & 0x7F)) << shift;

            if ((b & 0x80) == 0)
                return true;

            shift += 7;

            if (shift >= 64)
                return false;
        }

        return false;
    }

    private static bool TryReadFixed32Float(byte[] data, ref int index, out float value)
    {
        value = 0.0f;

        if (index + 4 > data.Length)
            return false;

        value = BitConverter.ToSingle(data, index);
        index += 4;

        return true;
    }

    private static bool TryReadLengthDelimited(byte[] data, ref int index, out byte[] result)
    {
        result = Array.Empty<byte>();

        if (!TryReadVarint(data, ref index, out ulong lengthValue))
            return false;

        if (lengthValue > int.MaxValue)
            return false;

        int length = (int)lengthValue;

        if (length < 0 || index + length > data.Length)
            return false;

        result = new byte[length];
        Buffer.BlockCopy(data, index, result, 0, length);
        index += length;

        return true;
    }

    private static bool IsSupportedWireType(int wireType)
    {
        return wireType == 0 || wireType == 1 || wireType == 2 || wireType == 5;
    }

    private static bool TrySkipField(byte[] data, ref int index, int wireType)
    {
        switch (wireType)
        {
            case 0:
                return TryReadVarint(data, ref index, out _);

            case 1:
                if (index + 8 > data.Length)
                    return false;

                index += 8;
                return true;

            case 2:
                if (!TryReadVarint(data, ref index, out ulong lengthValue))
                    return false;

                if (lengthValue > int.MaxValue)
                    return false;

                int length = (int)lengthValue;

                if (index + length > data.Length)
                    return false;

                index += length;
                return true;

            case 5:
                if (index + 4 > data.Length)
                    return false;

                index += 4;
                return true;

            default:
                return false;
        }
    }

    private static void SaveWeaponInfoForPlayer(
        CCSPlayerController player,
        WeaponTarget targetWeapon,
        ParsedInspectPreview preview
    )
    {
        var teamsToApply = player.TeamNum < 2
            ? new[] { CsTeam.Terrorist, CsTeam.CounterTerrorist }
            : new[] { player.Team };

        var playerSkins = GPlayerWeaponsInfo.GetOrAdd(
            player.Slot,
            _ => new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>()
        );

        foreach (var team in teamsToApply)
        {
            var teamWeapons = playerSkins.GetOrAdd(
                team,
                _ => new ConcurrentDictionary<int, WeaponInfo>()
            );

            teamWeapons[targetWeapon.DefIndex] = new WeaponInfo
            {
                Paint = preview.Paint,
                Seed = preview.Seed,
                Wear = preview.Wear,
                Nametag = "",
                StatTrak = preview.StatTrak,
                StatTrakCount = preview.StatTrakCount,
                Stickers = preview.Stickers,
                KeyChain = preview.KeyChain
            };
        }
    }

    private static void RemoveWeaponsInSameSlot(CCSPlayerController player, int slot)
    {
        var weapons = player.PlayerPawn.Value?.WeaponServices?.MyWeapons;

        if (weapons == null)
            return;

        foreach (var handle in weapons)
        {
            if (!handle.IsValid || handle.Value == null || !handle.Value.IsValid)
                continue;

            var weapon = handle.Value;
            string name = weapon.DesignerName;

            bool shouldRemove = (slot == 1 && PrimaryWeapons.Contains(name))
                             || (slot == 2 && SecondaryWeapons.Contains(name));

            if (!shouldRemove)
                continue;

            weapon.Remove();
        }
    }

    private static void SyncPlayerToDatabase(CCSPlayerController player)
    {
        if (WeaponSync == null)
            return;

        var playerInfo = new PlayerInfo
        {
            UserId = player.UserId,
            Slot = player.Slot,
            Index = (int)player.Index,
            SteamId = player.SteamID.ToString(),
            Name = player.PlayerName,
            IpAddress = player.IpAddress?.Split(":")[0]
        };

        _ = Task.Run(async () => await WeaponSync.SyncWeaponPaintsToDatabase(playerInfo));
    }
}
