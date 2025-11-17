using Core.ContributorAggregate;

using LUnit = LanguageExt.Unit;

namespace UseCases.Contributors.Delete;

public record DeleteContributorCommand(ContributorId ContributorId) : ICommand<Fin<LUnit>>;
