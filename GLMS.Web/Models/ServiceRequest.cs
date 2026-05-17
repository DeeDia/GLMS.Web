using System;
using System.ComponentModel.DataAnnotations;

namespace GLMS.Web.Models
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

        // Foreign key — links this request to a Contract
        [Required]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        // Navigation property
        public Contract? Contract { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        // User types this — amount in US dollars
        [Required]
        [Display(Name = "Cost (USD)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than zero")]
        public decimal CostUSD { get; set; }

        // Fetched from the currency API automatically
        [Display(Name = "Exchange Rate (USD → ZAR)")]
        public decimal ExchangeRate { get; set; }

        // Calculated: CostUSD × ExchangeRate
        [Display(Name = "Cost (ZAR)")]
        public decimal CostZAR { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [Display(Name = "Date Raised")]
        public DateTime DateRaised { get; set; } = DateTime.Now;
    }
}