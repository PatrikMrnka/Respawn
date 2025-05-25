namespace RespawnApi.Application.DTOs.Docker
{
    public class DockerImageDto
    {
        public required string Id { get; set; } // Krátké ID
        public string FullId { get; set; } = string.Empty; // Plné ID
        public List<string> RepoTags { get; set; } = new();
        public List<string> RepoDigests { get; set; } = new();
        public DateTime Created { get; set; }
        public long Size { get; set; } // Velikost v bajtech
        public long VirtualSize { get; set; }
        public Dictionary<string, string> Labels { get; set; } = new();
        public int Containers { get; set; } // Počet kontejnerů používajících tento image
    }
}
