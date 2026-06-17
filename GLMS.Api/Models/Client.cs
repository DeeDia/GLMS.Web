using System.ComponentModel.DataAnnotations;

namespace GLMS.Api.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Client Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Contact Details")]
        public string ContactDetails { get; set; } = string.Empty;

        [Required]
        public string Region { get; set; } = string.Empty;

        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}