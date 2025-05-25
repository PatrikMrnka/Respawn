using RespawnApi.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace RespawnApi.Application.DTOs.GameServer
{
    public class CreateGameServerDto
    {
        [Required(ErrorMessage = "Název serveru je povinný.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Název serveru musí mít 3-100 znaků.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Typ hry je povinný.")]
        public GameType GameType { get; set; }

        [StringLength(255, ErrorMessage = "Extra parametry nesmí být delší než 255 znaků.")]
        public string? AdditionalGsParams { get; set; }
    }
}
