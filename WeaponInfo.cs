namespace WeaponPaints
{
	public class WeaponInfo
	{
		public ushort Paint { get; set; }
		public ushort Seed { get; set; }
		public float Wear { get; set; }
        public string? NameTag { get; set; }
        public ushort Quality { get; set; }
        public uint StatTrack { get; set; }
        public bool StatTrackEnabled { get; set; }
    }
}