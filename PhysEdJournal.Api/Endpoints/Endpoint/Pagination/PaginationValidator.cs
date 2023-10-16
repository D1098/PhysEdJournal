using FluentValidation;

namespace PhysEdJournal.Api.Endpoints.Endpoint.Pagination;

public sealed class PaginationValidator<T> : AbstractValidator<T>
    where T : PaginationRequest
{
    public PaginationValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(PaginationRequest.MaxPageSize);
    }
}
