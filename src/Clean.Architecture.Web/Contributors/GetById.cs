using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors;
using Clean.Architecture.UseCases.Contributors.Get;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Clean.Architecture.Web.Contributors;

public class GetById(IMediator mediator)
  : Endpoint<GetContributorByIdRequest,
             Results<Ok<ContributorRecord>, NotFound, ProblemHttpResult>,
             GetContributorByIdMapper>
{
  public override void Configure()
  {
    Get(GetContributorByIdRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Get a contributor by ID";
      s.Description = "Retrieves a specific contributor by their unique identifier.";
      s.ExampleRequest = new GetContributorByIdRequest { ContributorId = 1 };
      s.ResponseExamples[200] = new ContributorRecord(1, "John Doe", "+1 555-555-5555");
      s.Responses[200] = "Contributor found";
      s.Responses[404] = "Contributor not found";
    });
    Tags("Contributors");
    Description(builder => builder
      .Accepts<GetContributorByIdRequest>()
      .Produces<ContributorRecord>(200, "application/json")
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<ContributorRecord>, NotFound, ProblemHttpResult>>
    ExecuteAsync(GetContributorByIdRequest request, CancellationToken ct)
  {
    var fin = await mediator.Send(new GetContributorQuery(ContributorId.From(request.ContributorId)), ct);

    if (fin.IsFail)
      return TypedResults.Problem(title: "Get failed", detail: fin.ToString(), statusCode: StatusCodes.Status400BadRequest);

    var opt = fin.Match(o => o, _ => Option<ContributorDto>.None);
    return opt.Match<Results<Ok<ContributorRecord>, NotFound, ProblemHttpResult>>(
      v => TypedResults.Ok(Map.FromEntity(v)),
      () => TypedResults.NotFound());
  }
}
public sealed class GetContributorByIdMapper
  : Mapper<GetContributorByIdRequest, ContributorRecord, ContributorDto>
{
  public override ContributorRecord FromEntity(ContributorDto e)
    => new(e.Id.Value, e.Name.Value, e.PhoneNumber.ToString());
}
