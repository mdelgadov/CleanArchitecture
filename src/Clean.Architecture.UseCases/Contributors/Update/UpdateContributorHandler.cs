using Core.ContributorAggregate;

namespace UseCases.Contributors.Update;

public class UpdateContributorHandler(IRepository<Contributor> _repository)
  : ICommandHandler<UpdateContributorCommand, Fin<Option<ContributorDto>>>
{
  public async ValueTask<Fin<Option<ContributorDto>>> Handle(UpdateContributorCommand command,
    CancellationToken ct)
  {
    var existingContributor = await _repository.GetByIdAsync(command.ContributorId, ct);
    if (existingContributor == null)
    {
      return Fin<Option<ContributorDto>>.Succ(Option<ContributorDto>.None);
    }

    existingContributor.UpdateName(command.NewName);

    await _repository.UpdateAsync(existingContributor, ct);

    return Fin<Option<ContributorDto>>.Succ(new ContributorDto(existingContributor.Id,
      existingContributor.Name, existingContributor.PhoneNumber ?? PhoneNumber.Unknown));
  }
}
