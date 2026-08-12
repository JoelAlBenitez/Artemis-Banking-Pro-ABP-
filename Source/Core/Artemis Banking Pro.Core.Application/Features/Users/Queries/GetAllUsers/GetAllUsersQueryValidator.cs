using Artemis_Banking_Pro.Core.Application.Common;
using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
    {
        public GetAllUsersQueryValidator()
        {
            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();

            RuleFor(query => query.Role)
                .Must(ApiFilterValues.User.IsAllowedRole)
                .WithMessage("El tipo de usuario solo puede ser Administrador, Cajero o Cliente.");
        }
    }
}
