using CornLimiter.Application.Commands;
using FluentValidation;

namespace CornLimiter.Application.Validators;

public class SellOneCommandValidator : AbstractValidator<SellOneCommand>
{
    public SellOneCommandValidator()
    {
        RuleFor(x => x.FarmerCode)
            .NotEmpty().WithMessage("Farmer code is required.")
            .NotEqual(Guid.Empty).WithMessage("Farmer code cannot be Guid.Empty.");
    }
}