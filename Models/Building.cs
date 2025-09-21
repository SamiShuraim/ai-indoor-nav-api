using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ai_indoor_nav_api.Models
{
    [Table("buildings")]
    public class Building
    {
        [Key]
        [Column("id")]
        public int Id { get; init; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = "";

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    }
}
