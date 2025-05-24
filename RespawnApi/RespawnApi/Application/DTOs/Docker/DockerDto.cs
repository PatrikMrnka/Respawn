// Application/DTOs/DockerAdmin/DockerVolumeDto.cs
namespace RespawnApi.Application.DTOs.DockerAdmin
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

// Application/DTOs/DockerAdmin/DockerContainerPortDto.cs
namespace RespawnApi.Application.DTOs.DockerAdmin
{
    public class DockerContainerPortDto
    {
        public ushort PrivatePort { get; set; }
        public ushort PublicPort { get; set; }
        public string Type { get; set; } = string.Empty; // tcp, udp
        public string? IP { get; set; }
    }
}

// Application/DTOs/DockerAdmin/DockerContainerDto.cs
namespace RespawnApi.Application.DTOs.DockerAdmin
{
    public class DockerContainerDto
    {
        public required string Id { get; set; }
        public List<string> Names { get; set; } = new();
        public required string Image { get; set; }
        public string ImageId { get; set; } = string.Empty;
        public required string Command { get; set; }
        public DateTime Created { get; set; }
        public List<DockerContainerPortDto> Ports { get; set; } = new();
        public required string State { get; set; } // např. "running", "exited"
        public required string Status { get; set; } // např. "Up 2 hours", "Exited (0) 5 minutes ago"
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}

// Application/DTOs/DockerAdmin/DockerImageDto.cs
namespace RespawnApi.Application.DTOs.DockerAdmin
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
