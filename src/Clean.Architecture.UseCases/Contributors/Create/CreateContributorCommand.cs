using Clean.Architecture.Core.ContributorAggregate;
using LanguageExt;

namespace Clean.Architecture.UseCases.Contributors.Create;

/// <summary>
/// Create a new Contributor.
/// </summary>
/// <param name="Name"></param>
public record CreateContributorCommand(ContributorName Name, string? PhoneNumber) : ICommand<Fin<ContributorId>>;
