namespace RespawnApi.Application.DTOs.Docker
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
