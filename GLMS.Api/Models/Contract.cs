using System.ComponentModel.DataAnnotations;

namespace GLMS.Api.Models
{
    public enum ContractStatus
    {
        Draft,
        Active,
        Expired,
        OnHold
    }

    public class Contract
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        public Client? Client { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [Required]
        public string ServiceLevel { get; set; } = string.Empty;

        public string? SignedAgreementPath { get; set; }

        public ICollection<ServiceRequest> ServiceRequests { get; set; }
            = new List<ServiceRequest>();
    }
}