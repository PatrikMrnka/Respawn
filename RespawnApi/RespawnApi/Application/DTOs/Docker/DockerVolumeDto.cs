namespace RespawnApi.Application.DTOs.Docker
{
    public class DockerVolumeDto
    {
        public required string Name { get; set; }
        public string Driver { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; } // Velikost ve bajtech, pokud je dostupná
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}
