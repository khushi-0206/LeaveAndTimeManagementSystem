using AuthService.Application.DTOs;
using AuthService.Domain.Enums;
using FluentValidation;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AuthService.Application.Validators
{
    public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100);

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.");

            RuleFor(x => x.Role)
                .NotEmpty()
                .Must(r => r == UserRoles.Employee || r == UserRoles.Manager || r == UserRoles.HRAdmin)
                .WithMessage("Role must be Employee, Manager, or HRAdmin.");

            RuleFor(x => x.Department).NotEmpty();
            RuleFor(x => x.Designation).NotEmpty();
            RuleFor(x => x.DateOfJoining).NotEmpty();
            RuleFor(x => x.EmploymentType).NotEmpty();
        }
    }
}
