namespace RespawnApi.Application.DTOs.Docker
{
    /// <summary>
    /// Represents a Docker volume data transfer object.
    /// </summary>
    public class DockerVolumeDto
    {
        /// <summary>
        /// The unique identifier of the Docker volume.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The driver used for the Docker volume, such as "local" or "nfs".
        /// </summary>
        public string Driver { get; set; } = string.Empty;

        /// <summary>
        /// The mount point of the Docker volume on the host machine.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The date and time when the Docker volume was created - if available.
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// The labels associated with the Docker volume, which are key-value pairs providing metadata about the volume.
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}
