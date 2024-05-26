namespace WeaponPaints
{
	public class WeaponInfo
	{
		public ushort Paint { get; set; }
		public ushort Seed { get; set; } = 0;
		public float Wear { get; set; } = 0f;
        public string? NameTag { get; set; }
        public ushort Quality { get; set; }
        public uint StatTrack { get; set; }
        public bool StatTrackEnabled { get; set; }
    }
}