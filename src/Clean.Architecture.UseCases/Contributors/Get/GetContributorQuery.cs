using Clean.Architecture.Core.ContributorAggregate;

namespace Clean.Architecture.UseCases.Contributors.Get;

public record GetContributorQuery(ContributorId ContributorId) : IQuery<Fin<Option<ContributorDto>>>;
