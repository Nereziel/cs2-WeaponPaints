namespace WeaponPaints
{
	public class PlayerInfo
	{
		public int Index { get; set; }
		public int Slot { get; init; }
		public int? UserId { get; set; }
        public ulong? SteamId { get; init; }
		public string? Name { get; set; }
		public string? IpAddress { get; set; }
	}
}