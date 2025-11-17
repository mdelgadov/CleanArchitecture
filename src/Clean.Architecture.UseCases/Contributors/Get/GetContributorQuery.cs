using Core.ContributorAggregate;

namespace UseCases.Contributors.Get;

public record GetContributorQuery(ContributorId ContributorId) : IQuery<Fin<Option<ContributorDto>>>;
