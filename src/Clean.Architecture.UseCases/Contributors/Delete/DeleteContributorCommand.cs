using Clean.Architecture.Core.ContributorAggregate;
using LUnit = LanguageExt.Unit;

namespace Clean.Architecture.UseCases.Contributors.Delete;

public record DeleteContributorCommand(ContributorId ContributorId) : ICommand<Fin<LUnit>>;
