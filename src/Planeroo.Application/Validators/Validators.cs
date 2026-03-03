using FluentValidation;
using Planeroo.Application.Features.Children.Commands;
using Planeroo.Application.Features.Homework.Commands;

namespace Planeroo.Application.Validators;

public class CreateChildValidator : AbstractValidator<CreateChildCommand>
{
    public CreateChildValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must be less than 50 characters");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .Must(dob => dob < DateTime.UtcNow.AddYears(-5))
            .WithMessage("Child must be at least 5 years old")
            .Must(dob => dob > DateTime.UtcNow.AddYears(-18))
            .WithMessage("Child must be under 18 years old");

        RuleFor(x => x.GradeLevel)
            .InclusiveBetween(1, 12).WithMessage("Grade level must be between 1 and 12");

        RuleFor(x => x.Pin)
            .Matches(@"^\d{4}$").When(x => x.Pin != null)
            .WithMessage("PIN must be exactly 4 digits");
    }
}

public class CreateHomeworkValidator : AbstractValidator<CreateHomeworkCommand>
{
    public CreateHomeworkValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must be less than 200 characters");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future");

        RuleFor(x => x.EstimatedMinutes)
            .InclusiveBetween(5, 480).WithMessage("Estimated time must be between 5 and 480 minutes")
            .When(x => x.EstimatedMinutes.HasValue);

        RuleFor(x => x.ChildId)
            .NotEmpty().WithMessage("Child ID is required");
    }
}
