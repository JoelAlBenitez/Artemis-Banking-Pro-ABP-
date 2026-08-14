using Artemis_Banking_Pro.Core.Application.Common;
using FluentValidation;

namespace Artemis_Banking_Pro.Core.Application.Features.Commerces.Queries.GetAllCommerces
{
    public class GetAllCommercesQueryValidator : AbstractValidator<GetAllCommercesQuery>
    {
        public GetAllCommercesQueryValidator()
        {
            RuleFor(query => query.Page).ValidPage();
            RuleFor(query => query.PageSize).ValidPageSize();

            RuleFor(query => query.Status)
                .Must(ApiFilterValues.Commerce.IsAllowed)
                .WithMessage("El estado solo puede ser activo, inactivo o todos.");
        }
    }
}
