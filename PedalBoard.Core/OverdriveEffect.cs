namespace PedalBoard.Core
{
    public class OverdriveEffect : DistortionEffect
    {
        public OverdriveEffect() : base("Overdrive")
        {
            HardClipping = false; // Soft clipping for overdrive
            Drive = 0.5f;
        }
    }
}
