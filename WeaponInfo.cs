namespace WeaponPaints
{
    public class WeaponInfo
    {
        public int Paint { get; set; }
        public int Seed { get; set; }
        public float Wear { get; set; }

        public WeaponInfo() : this(0, 0, 0f) { }

        public WeaponInfo(int paint, int seed, float wear)
        {
            Paint = paint;
            Seed = seed;
            Wear = wear;
        }
    }
}