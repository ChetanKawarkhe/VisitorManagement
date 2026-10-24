using FluentValidation;
using VisitorManagement.Application.DTOs.Visitors;
using VisitorManagement.Domain.Enums;

namespace VisitorManagement.Application.Validators;

public sealed class UpdateVisitorRequestValidator
    : AbstractValidator<UpdateVisitorRequest>
{
    public UpdateVisitorRequestValidator()
    {
        RuleFor(x => x.VisitorName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.MobileNumber)
            .NotEmpty()
            .Matches(@"^\+?[0-9]{10,15}$")
            .WithMessage(
                "Mobile number must contain 10 to 15 digits and may start with +.");

        RuleFor(x => x.Email)
            .MaximumLength(254)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.VisitDate)
            .NotEqual(default(DateTime))
            .WithMessage("Visit date is required.");

        RuleFor(x => x.HostId)
            .GreaterThan(0);

        RuleFor(x => x.PurposeId)
            .GreaterThan(0);

        RuleFor(x => x.Remarks)
            .MaximumLength(500);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(status => Enum.TryParse<VisitorStatus>(
                status,
                true,
                out var parsed) &&
                Enum.IsDefined(typeof(VisitorStatus), parsed))
            .WithMessage("Invalid visitor status.");

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .Must(IsValidRowVersion)
            .WithMessage("A valid row version is required.");
    }

    private static bool IsValidRowVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            return Convert.FromBase64String(value).Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}