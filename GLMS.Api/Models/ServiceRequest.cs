using System.ComponentModel.DataAnnotations;

namespace GLMS.Api.Models
{
    public enum RequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public int ContractId { get; set; }
        public Contract? Contract { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal CostUSD { get; set; }

        public decimal ExchangeRate { get; set; }
        public decimal CostZAR { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        public DateTime DateRaised { get; set; } = DateTime.Now;
    }
}