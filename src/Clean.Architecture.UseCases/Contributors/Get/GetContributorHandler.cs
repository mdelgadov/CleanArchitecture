using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.Core.ContributorAggregate.Specifications;

namespace Clean.Architecture.UseCases.Contributors.Get;

/// <summary>
/// Queries don't necessarily need to use repository methods, but they can if it's convenient
/// </summary>
public class GetContributorHandler(IReadRepository<Contributor> _repository)
  : IQueryHandler<GetContributorQuery, Fin<Option<ContributorDto>>>
{
  public async ValueTask<Fin<Option<ContributorDto>>> Handle(GetContributorQuery request, CancellationToken cancellationToken)
  {
    var spec = new ContributorByIdSpec(request.ContributorId);
    var entity = await _repository.FirstOrDefaultAsync(spec, cancellationToken);
    if (entity == null) return Fin<Option<ContributorDto>>.Succ(Option<ContributorDto>.None);

    return Fin<Option<ContributorDto>>.Succ(new ContributorDto(entity.Id, entity.Name, entity.PhoneNumber ?? PhoneNumber.Unknown));
  }
}
