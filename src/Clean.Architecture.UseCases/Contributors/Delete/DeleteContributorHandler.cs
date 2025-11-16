using Clean.Architecture.Core.Interfaces;
using LUnit = LanguageExt.Unit;

namespace Clean.Architecture.UseCases.Contributors.Delete;

public class DeleteContributorHandler(IDeleteContributorService _deleteContributorService)
  : ICommandHandler<DeleteContributorCommand, Fin<LUnit>>
{
  public async ValueTask<Fin<LUnit>> Handle(DeleteContributorCommand request, CancellationToken cancellationToken)
  {
    var result = await _deleteContributorService.DeleteContributor(request.ContributorId);
    return result.IsSuccess ? Fin<LUnit>.Succ(LUnit.Default) : Fin<LUnit>.Fail(result.Errors.FirstOrDefault() ?? "Delete failed");
  }
}
