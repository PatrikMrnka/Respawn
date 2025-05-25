namespace RespawnApi.Application.DTOs.Docker
{
    /// <summary>
    /// Represents a Docker image data transfer object.
    /// </summary>
    public class DockerImageDto
    {
        /// <summary>
        /// The unique identifier of the Docker image.
        /// </summary>
        public required string Id { get; set; } 

        /// <summary>
        /// The full identifier of the Docker image, which includes the repository and tag.
        /// </summary>
        public string FullId { get; set; } = string.Empty;

        /// <summary>
        /// A list of names associated with the Docker image, typically including the repository and tag.
        /// </summary>
        public List<string> RepoTags { get; set; } = new();

        /// <summary>
        /// A list of digests associated with the Docker image, which are unique identifiers for the image content.
        /// </summary>
        public List<string> RepoDigests { get; set; } = new();

        /// <summary>
        /// The command that was used to create the Docker image.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// The size of the Docker image in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The virtual size of the Docker image, which may include the size of parent images.
        /// </summary>
        public long VirtualSize { get; set; }

        /// <summary>
        /// A list of labels associated with the Docker image, which are key-value pairs providing metadata about the image.
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();

        /// <summary>
        /// The number of containers that are currently using this Docker image.
        /// </summary>
        public int Containers { get; set; }
    }
}
