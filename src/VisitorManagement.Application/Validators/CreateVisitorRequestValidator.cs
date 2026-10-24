using FluentValidation;
using VisitorManagement.Application.DTOs.Visitors;

namespace VisitorManagement.Application.Validators;

public sealed class CreateVisitorRequestValidator
    : AbstractValidator<CreateVisitorRequest>
{
    public CreateVisitorRequestValidator()
    {
        RuleFor(x => x.VisitorName)
            .NotEmpty()
            .WithMessage("Visitor name is required.")
            .MaximumLength(100);

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithMessage("Company name is required.")
            .MaximumLength(150);

        RuleFor(x => x.MobileNumber)
            .NotEmpty()
            .WithMessage("Mobile number is required.")
            .Matches(@"^\+?[0-9]{10,15}$")
            .WithMessage(
                "Mobile number must contain 10 to 15 digits and may start with +.");

        RuleFor(x => x.Email)
            .MaximumLength(254)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.VisitDate)
            .Must(date => date != default)
            .WithMessage("Visit date is required.")
            .Must(date => date >= DateTime.Now)
            .WithMessage("Visit date cannot be in the past.");

        RuleFor(x => x.HostId)
            .GreaterThan(0)
            .WithMessage("Please select a host.");

        RuleFor(x => x.PurposeId)
            .GreaterThan(0)
            .WithMessage("Please select a visit purpose.");

        RuleFor(x => x.Remarks)
            .MaximumLength(500);
    }
}