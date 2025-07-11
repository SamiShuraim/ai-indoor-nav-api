using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_indoor_nav_api.Models
{
    public class Edge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FromNodeId { get; set; }

        [Required]
        public int ToNodeId { get; set; }

        [ForeignKey("FromNodeId")]
        public Node? FromNode { get; set; }

        [ForeignKey("ToNodeId")]
        public Node? ToNode { get; set; }
    }
}