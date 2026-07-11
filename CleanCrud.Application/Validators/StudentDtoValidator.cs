using CleanCrud.Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CleanCrud.Application.Validators
{
    public class StudentDtoValidator : AbstractValidator<StudentDto>
    {
        public StudentDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Invalid email format");

            RuleFor(x => x.MobileNo)
                .NotEmpty()
                .WithMessage("Mobile number is required")
                .Length(10)
                .WithMessage("Mobile number must be 10 digits");
        }
    }
}
