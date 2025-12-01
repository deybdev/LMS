using System;

namespace LMS.Models
{
    public class MaterialComment
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Material Material { get; set; }
        public virtual User User { get; set; }
    }
}
