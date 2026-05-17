using System;
using GLMS.Web.Models;
using GLMS.Web.Services;
using Xunit;

namespace GLMS.Tests
{
    public class BusinessLogicTests
    {
        private readonly ContractValidationService _service;

        public BusinessLogicTests()
        {
            _service = new ContractValidationService();
        }

        // CURRENCY TESTS
        
        [Fact]
        public void CurrencyConversion_CorrectRate_ReturnsCorrectZar()
        {
            // ARRANGE
            decimal usdAmount = 100m;
            decimal exchangeRate = 18.50m;
            decimal expected = 1850.00m;

            // ACT
            decimal result = _service.CalculateZarAmount(
                usdAmount, exchangeRate);

            // ASSERT
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CurrencyConversion_DecimalAmount_RoundsToTwoPlaces()
        {
            // ARRANGE
            // 33.33 × 18.50 = 616.605
            // With MidpointRounding.AwayFromZero → 616.61
            decimal usdAmount = 33.33m;
            decimal exchangeRate = 18.50m;
            decimal expected = 616.61m;

            // ACT
            decimal result = _service.CalculateZarAmount(
                usdAmount, exchangeRate);

            // ASSERT
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CurrencyConversion_ZeroAmount_ThrowsArgumentException()
        {
            // ARRANGE
            decimal usdAmount = 0m;
            decimal exchangeRate = 18.50m;

            // ACT + ASSERT
            // Checks the method throws when given zero
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateZarAmount(usdAmount, exchangeRate));

            Assert.Contains("greater than zero", ex.Message);
        }

        [Fact]
        public void CurrencyConversion_NegativeRate_ThrowsArgumentException()
        {
            // ARRANGE
            decimal usdAmount = 100m;
            decimal exchangeRate = -1m;

            // ACT + ASSERT
            var ex = Assert.Throws<ArgumentException>(() =>
                _service.CalculateZarAmount(usdAmount, exchangeRate));

            Assert.Contains("greater than zero", ex.Message);
        }

        // FILE VALIDATION TESTS
        
        [Fact]
        public void FileValidation_PdfFile_DoesNotThrow()
        {
            // ARRANGE
            var fileName = "signed_agreement.pdf";

            // ACT
            // Record.Exception returns null if NO exception is thrown
            var exception = Record.Exception(() =>
                _service.ValidateFileUpload(fileName));

            // ASSERT — null means no exception = test passes
            Assert.Null(exception);
        }

        [Fact]
        public void FileValidation_ExeFile_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var fileName = "virus.exe";

            // ACT + ASSERT
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _service.ValidateFileUpload(fileName));

            Assert.Contains(".exe", ex.Message);
        }

        [Fact]
        public void FileValidation_DocxFile_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var fileName = "contract.docx";

            // ACT + ASSERT
            Assert.Throws<InvalidOperationException>(() =>
                _service.ValidateFileUpload(fileName));
        }

        [Fact]
        public void FileValidation_EmptyFileName_ThrowsArgumentException()
        {
            // ARRANGE
            var fileName = "";

            // ACT + ASSERT
            Assert.Throws<ArgumentException>(() =>
                _service.ValidateFileUpload(fileName));
        }

        // WORKFLOW GUARD TESTS
        
        [Fact]
        public void WorkflowGuard_ActiveContract_DoesNotThrow()
        {
            // ARRANGE — build a fake Active contract
            // No database needed — just a plain C# object
            var contract = new GLMS.Web.Models.Contract
            {
                Id = 1,
                Status = ContractStatus.Active,
                ServiceLevel = "Premium",
                ClientId = 1,
                StartDate = DateTime.Now.AddMonths(-1),
                EndDate = DateTime.Now.AddMonths(11)
            };

            // ACT
            var exception = Record.Exception(() =>
                _service.ValidateContractForRequest(contract));

            // ASSERT — Active contract allows requests
            Assert.Null(exception);
        }

        [Fact]
        public void WorkflowGuard_ExpiredContract_ThrowsInvalidOperationException()
        {
            var contract = new GLMS.Web.Models.Contract
            {
                Id = 2,
                Status = ContractStatus.Expired,
                ServiceLevel = "Standard",
                ClientId = 1,
                StartDate = DateTime.Now.AddMonths(-12),
                EndDate = DateTime.Now.AddMonths(-1)
            };

            // ACT + ASSERT — must throw
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _service.ValidateContractForRequest(contract));

            Assert.Contains("Expired", ex.Message);
        }

        [Fact]
        public void WorkflowGuard_OnHoldContract_ThrowsInvalidOperationException()
        {
            // ARRANGE
            var contract = new GLMS.Web.Models.Contract
            {
                Id = 3,
                Status = ContractStatus.OnHold,
                ServiceLevel = "Basic",
                ClientId = 1,
                StartDate = DateTime.Now.AddMonths(-6),
                EndDate = DateTime.Now.AddMonths(6)
            };

            // ACT + ASSERT — must throw
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _service.ValidateContractForRequest(contract));

            Assert.Contains("On Hold", ex.Message);
        }

        [Fact]
        public void WorkflowGuard_DraftContract_DoesNotThrow()
        {
            // ARRANGE — Draft contracts CAN have requests raised
            var contract = new GLMS.Web.Models.Contract
            {
                Id = 4,
                Status = ContractStatus.Draft,
                ServiceLevel = "Basic",
                ClientId = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(12)
            };

            // ACT
            var exception = Record.Exception(() =>
                _service.ValidateContractForRequest(contract));

            // ASSERT
            Assert.Null(exception);
        }

        [Fact]
        public void WorkflowGuard_NullContract_ThrowsArgumentNullException()
        {
            // ARRANGE
            GLMS.Web.Models.Contract? contract = null;

            // ACT + ASSERT
            Assert.Throws<ArgumentNullException>(() =>
                _service.ValidateContractForRequest(contract!));
        }
    }
}