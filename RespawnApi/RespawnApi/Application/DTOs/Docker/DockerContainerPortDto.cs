namespace RespawnApi.Application.DTOs.Docker
{
    public class DockerContainerPortDto
    {
        public ushort PrivatePort { get; set; }
        public ushort PublicPort { get; set; }
        public string Type { get; set; } = string.Empty; // tcp, udp
        public string? IP { get; set; }
    }
}
