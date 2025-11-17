namespace UseCases.Contributors.List;

public record ListContributorsQuery(int? Page = 1, int? PerPage = Constants.DEFAULT_PAGE_SIZE)
  : IQuery<Fin<PagedResult<ContributorDto>>>;
