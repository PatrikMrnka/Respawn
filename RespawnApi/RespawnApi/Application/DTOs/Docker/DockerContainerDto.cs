namespace RespawnApi.Application.DTOs.Docker
{
    /// <summary>
    /// Represents a Docker container data transfer object.
    /// </summary>
    public class DockerContainerDto
    {
        /// <summary>
        /// The unique identifier of the Docker container.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// A list of names associated with the Docker container.
        /// </summary>
        public List<string> Names { get; set; } = new();

        /// <summary>
        /// The image used to create the Docker container.
        /// </summary>
        public required string Image { get; set; }

        /// <summary>
        /// The unique identifier of the Docker image used to create the container.
        /// </summary>
        public string ImageId { get; set; } = string.Empty;

        /// <summary>
        /// The command that was used to start the Docker container.
        /// </summary>
        public required string Command { get; set; }

        /// <summary>
        /// The date and time when the Docker container was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// A list of ports exposed by the Docker container.
        /// </summary>
        public List<DockerContainerPortDto> Ports { get; set; } = new();

        /// <summary>
        /// The state of the Docker container, such as "running" or "exited".
        /// </summary>
        public required string State { get; set; } // např. "running", "exited"

        /// <summary>
        /// The status of the Docker container, providing additional details about its state.
        /// </summary>
        public required string Status { get; set; } // např. "Up 2 hours", "Exited (0) 5 minutes ago"

        /// <summary>
        /// The URL of the Docker container's image.
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}
