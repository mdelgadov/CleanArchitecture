using Clean.Architecture.Core.ContributorAggregate;
using LanguageExt;

namespace Clean.Architecture.UseCases.Contributors.Update;

public record UpdateContributorCommand(ContributorId ContributorId, ContributorName NewName) : ICommand<Fin<Option<ContributorDto>>>;
