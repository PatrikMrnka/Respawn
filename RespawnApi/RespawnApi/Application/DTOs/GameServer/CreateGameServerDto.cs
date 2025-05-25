using RespawnApi.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.GameServer
{
    /// <summary>
    /// Represents the data transfer object for creating a new game server.
    /// </summary>
    public class CreateGameServerDto
    {
        /// <summary>
        /// The unique identifier for the game server, typically a GUID.
        /// </summary>
        [Required(ErrorMessage = "Název serveru je povinný.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Název serveru musí mít 3-100 znaků.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier for the game server's Docker image, typically a GUID.
        /// </summary>

        [Required(ErrorMessage = "Typ hry je povinný.")]
        public GameType GameType { get; set; }

        /// <summary>
        /// The unique identifier for the game server's Docker image, typically a GUID.
        /// </summary>

        [StringLength(255, ErrorMessage = "Extra parametry nesmí být delší než 255 znaků.")]
        public string? AdditionalGsParams { get; set; }
    }
}
