namespace WeaponPaints
{
    public class PlayerInfo
    {
        public int Index { get; set; }
        public int Slot { get; set; }
        public int? UserId { get; set; }
        public string? SteamId { get; set; }
        public string? Name { get; set; }
        public string? IpAddress { get; set; }

        public PlayerInfo() { }

        public PlayerInfo(int index, int slot, int? userId, string? steamId, string? name, string? ipAddress)
        {
            Index = index;
            Slot = slot;
            UserId = userId;
            SteamId = steamId;
            Name = name;
            IpAddress = ipAddress;
        }
    }
}