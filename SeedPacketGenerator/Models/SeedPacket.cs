namespace SeedPacketGenerator.Models
{
    public class SeedPacket
    {
        public string SeedName { get; set; } = "Seed Name";
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
        public string ?BackgroundImage { get; set; } = ""; // Default to none
    }
}