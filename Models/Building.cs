using System.ComponentModel.DataAnnotations;

namespace ai_indoor_nav_api.Models
{
    public class Building
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public ICollection<Floor> Floors { get; set; } = new List<Floor>();
    }
}