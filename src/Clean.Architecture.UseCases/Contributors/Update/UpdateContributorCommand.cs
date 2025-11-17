using Core.ContributorAggregate;

namespace UseCases.Contributors.Update;

public record UpdateContributorCommand(ContributorId ContributorId, ContributorName NewName) : ICommand<Fin<Option<ContributorDto>>>;
