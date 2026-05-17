using System;
using GLMS.Web.Models;

namespace GLMS.Web.Services
{
    public class ContractValidationService
    {
        public void ValidateContractForRequest(
            GLMS.Web.Models.Contract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(
                    nameof(contract),
                    "Contract cannot be null.");

            if (contract.Status == ContractStatus.Expired)
                throw new InvalidOperationException(
                    "Cannot raise a service request against an " +
                    "Expired contract.");

            if (contract.Status == ContractStatus.OnHold)
                throw new InvalidOperationException(
                    "Cannot raise a service request against a " +
                    "contract that is On Hold.");
        }

        public void ValidateFileUpload(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException(
                    "File name cannot be empty.");

            var extension = System.IO.Path
                .GetExtension(fileName)
                .ToLowerInvariant();

            if (extension != ".pdf")
                throw new InvalidOperationException(
                    $"Only PDF files are allowed. " +
                    $"You uploaded: {extension}");
        }
        public decimal CalculateZarAmount(
            decimal usdAmount, decimal exchangeRate)
        {
            if (usdAmount <= 0)
                throw new ArgumentException(
                    "USD amount must be greater than zero.");

            if (exchangeRate <= 0)
                throw new ArgumentException(
                    "Exchange rate must be greater than zero.");

            // MidpointRounding.AwayFromZero means .5 always rounds UP
            // This matches standard financial rounding rules
            return Math.Round(
                usdAmount * exchangeRate,
                2,
                MidpointRounding.AwayFromZero);
        }
    }
}