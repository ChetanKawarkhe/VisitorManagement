using VisitorManagement.Application.DTOs.Visitors;
using VisitorManagement.Application.Validators;

namespace VisitorManagement.UnitTests.Validators;

public sealed class CreateVisitorRequestValidatorTests
{
    private readonly CreateVisitorRequestValidator _validator = new();

    private static CreateVisitorRequest CreateValidRequest()
    {
        return new CreateVisitorRequest
        {
            VisitorName = "Rahul Sharma",
            CompanyName = "ABC Technologies",
            MobileNumber = "9876543210",
            Email = "rahul@example.com",
            VisitDate = DateTime.Now.AddDays(2),
            HostId = 1,
            PurposeId = 1,
            Remarks = "Business meeting"
        };
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(
            result.IsValid,
            string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Validate_EmptyVisitorName_ReturnsError()
    {
        var request = CreateValidRequest();
        request.VisitorName = "";

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.VisitorName));
    }

    [Fact]
    public void Validate_VisitorNameExceeds100Characters_ReturnsError()
    {
        var request = CreateValidRequest();
        request.VisitorName = new string('A', 101);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.VisitorName));
    }

    [Fact]
    public void Validate_EmptyCompanyName_ReturnsError()
    {
        var request = CreateValidRequest();
        request.CompanyName = "";

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.CompanyName));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    [InlineData("abcdefghij")]
    [InlineData("98765-43210")]
    public void Validate_InvalidMobileNumber_ReturnsError(string mobile)
    {
        var request = CreateValidRequest();
        request.MobileNumber = mobile;

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.MobileNumber));
    }

    [Theory]
    [InlineData("9876543210")]
    [InlineData("+919876543210")]
    [InlineData("123456789012345")]
    public void Validate_ValidMobileNumber_ReturnsSuccess(string mobile)
    {
        var request = CreateValidRequest();
        request.MobileNumber = mobile;

        var result = _validator.Validate(request);

        Assert.DoesNotContain(
            result.Errors,
            error => error.PropertyName == nameof(request.MobileNumber));
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsError()
    {
        var request = CreateValidRequest();
        request.Email = "invalid-email";

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.Email));
    }

    [Fact]
    public void Validate_OptionalEmailIsEmpty_ReturnsSuccess()
    {
        var request = CreateValidRequest();
        request.Email = "";

        var result = _validator.Validate(request);

        Assert.DoesNotContain(
            result.Errors,
            error => error.PropertyName == nameof(request.Email));
    }

    [Fact]
    public void Validate_PastVisitDate_ReturnsError()
    {
        var request = CreateValidRequest();
        request.VisitDate = DateTime.Now.AddDays(-1);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.VisitDate));
    }

    [Fact]
    public void Validate_MissingHost_ReturnsError()
    {
        var request = CreateValidRequest();
        request.HostId = 0;

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.HostId));
    }

    [Fact]
    public void Validate_MissingPurpose_ReturnsError()
    {
        var request = CreateValidRequest();
        request.PurposeId = 0;

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.PurposeId));
    }

    [Fact]
    public void Validate_RemarksExceed500Characters_ReturnsError()
    {
        var request = CreateValidRequest();
        request.Remarks = new string('R', 501);

        var result = _validator.Validate(request);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(request.Remarks));
    }
}