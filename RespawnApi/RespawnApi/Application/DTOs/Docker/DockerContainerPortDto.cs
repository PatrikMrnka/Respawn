namespace RespawnApi.Application.DTOs.Docker
{
    /// <summary>
    /// Represents a port mapping for a Docker container.
    /// </summary>
    public class DockerContainerPortDto
    {
        /// <summary>
        /// The private port of the Docker container, which is the port inside the container.
        /// </summary>
        public ushort PrivatePort { get; set; }

        /// <summary>
        /// The public port of the Docker container, which is the port exposed to the host machine.
        /// </summary>
        public ushort PublicPort { get; set; }

        /// <summary>
        /// The type of the port, such as "tcp" or "udp".
        /// </summary>
        public string Type { get; set; } = string.Empty; // tcp, udp

        /// <summary>
        /// The IP address associated with the port mapping, if applicable.
        /// </summary>
        public string? IP { get; set; }
    }
}
