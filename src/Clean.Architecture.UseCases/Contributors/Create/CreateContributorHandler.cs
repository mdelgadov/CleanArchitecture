using Core.ContributorAggregate;

namespace UseCases.Contributors.Create;

public class CreateContributorHandler(IRepository<Contributor> _repository)
  : ICommandHandler<CreateContributorCommand, Fin<ContributorId>>
{
  public async ValueTask<Fin<ContributorId>> Handle(CreateContributorCommand command,
    CancellationToken cancellationToken)
  {
    var newContributor = new Contributor(command.Name);
    if (!string.IsNullOrEmpty(command.PhoneNumber))
    {
      var phoneNumber = new PhoneNumber("+1", command.PhoneNumber, string.Empty);
      newContributor.UpdatePhoneNumber(phoneNumber);
    }
    var createdItem = await _repository.AddAsync(newContributor, cancellationToken);

    return Fin<ContributorId>.Succ(createdItem.Id);
  }
}
