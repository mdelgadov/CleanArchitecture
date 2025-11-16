using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors.Delete;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Clean.Architecture.Web.Contributors;

public class Delete : Endpoint<DeleteContributorRequest, Results<NoContent, ProblemHttpResult>>
{
  private readonly IMediator _mediator;
  public Delete(IMediator mediator) => _mediator = mediator;

  public override void Configure()
  {
    Delete(DeleteContributorRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Delete a contributor";
      s.Description = "Deletes an existing contributor by ID.";
      s.ExampleRequest = new DeleteContributorRequest { ContributorId = 1 };
      s.Responses[204] = "Contributor deleted successfully";
      s.Responses[400] = "Invalid request or deletion failed";
    });
    Tags("Contributors");
    Description(builder => builder
      .Accepts<DeleteContributorRequest>()
      .Produces(204)
      .ProducesProblem(400));
  }

  public override async Task<Results<NoContent, ProblemHttpResult>> ExecuteAsync(DeleteContributorRequest req, CancellationToken ct)
  {
    var fin = await _mediator.Send(new DeleteContributorCommand(ContributorId.From(req.ContributorId)), ct);
    return fin.Match<Results<NoContent, ProblemHttpResult>>(
      _ => TypedResults.NoContent(),
      err => TypedResults.Problem(title: "Delete failed", detail: err.ToString(), statusCode: StatusCodes.Status400BadRequest));
  }
}
