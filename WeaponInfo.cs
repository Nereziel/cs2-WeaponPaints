namespace WeaponPaints
{
	public class WeaponInfo
	{
		public int Paint { get; set; }
		public int Seed { get; set; }
		public float Wear { get; set; }
		public string Nametag { get; set; } = "";
		public bool StatTrak { get; set; }
		public int StatTrakCount { get; set; }
		public KeyChainInfo? KeyChain { get; set; }
		public List<StickerInfo> Stickers { get; set; } = new();
	}

	public class StickerInfo
	{
		public uint Id { get; set; }
		public uint Schema { get; set; }
		public float OffsetX { get; set; }
		public float OffsetY { get; set; }
		public float Wear { get; set; }
		public float Scale { get; set; }
		public float Rotation { get; set; }
	}

	public class KeyChainInfo
	{
		public uint Id { get; set; }
		public float OffsetX { get; set; }
		public float OffsetY { get; set; }
		public float OffsetZ { get; set; }
		public uint Seed { get; set; }
	}
}