using Apcloudpms.Application.DTOs;
using FluentValidation;

namespace Apcloudpms.Application.Validators;

public sealed class RiskAssessmentRequestDtoValidator : AbstractValidator<RiskAssessmentRequestDto>
{
    public RiskAssessmentRequestDtoValidator()
    {
        RuleFor(x => x.IssueDate).NotEqual(default(DateOnly));
        RuleFor(x => x.PlannedEndDateTime)
            .GreaterThanOrEqualTo(x => x.PlannedStartDateTime)
            .When(x => x.PlannedStartDateTime.HasValue && x.PlannedEndDateTime.HasValue)
            .WithMessage("Planned end date/time must be on or after the planned start date/time.");

        ValidateSelections(x => x.AdditionalPpe);
        ValidateSelections(x => x.HazardCategories);
        ValidateSelections(x => x.PersonalProtectiveEquipment);
        ValidateSelections(x => x.SpecialPermits);
    }

    private void ValidateSelections(
        System.Linq.Expressions.Expression<Func<RiskAssessmentRequestDto,
            IEnumerable<RiskAssessmentSelectionDto>>> expression)
    {
        RuleForEach(expression).ChildRules(selection =>
            selection.RuleFor(x => x.ListItemId).GreaterThan(0));
        RuleFor(expression).Must(items => items is null ||
                items.Select(x => x.ListItemId).Distinct().Count() == items.Count())
            .WithMessage("A list item may only appear once in each selection collection.");
    }
}
