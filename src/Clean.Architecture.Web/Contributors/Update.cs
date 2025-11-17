using Microsoft.AspNetCore.Http.HttpResults;

using UseCases.Contributors;
using UseCases.Contributors.Update;

using ContributorId = Core.ContributorAggregate.ContributorId;
using ContributorName = Core.ContributorAggregate.ContributorName;
// for ContributorDto

namespace Web.Contributors;

public class Update(IMediator mediator)
  : Endpoint<UpdateContributorRequest, Results<Ok<UpdateContributorResponse>, NotFound, ProblemHttpResult>, UpdateContributorMapper>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Put(UpdateContributorRequest.Route);
    AllowAnonymous();

    // Optional but nice: enumerate for Swagger
    Summary(s =>
    {
      s.Summary = "Update a contributor";
      s.Description = "Updates an existing contributor's information.";
      s.ExampleRequest = new UpdateContributorRequest { Id = 1, Name = "Updated Name" };
      s.ResponseExamples[200] = new UpdateContributorResponse(new ContributorRecord(1, "Updated Name", ""));

      // Document possible responses
      s.Responses[200] = "Contributor updated successfully";
      s.Responses[404] = "Contributor with specified ID not found";
      s.Responses[400] = "Invalid input data";
    });

    // Add tags for API grouping
    Tags("Contributors");

    // Add additional metadata
    Description(builder => builder
      .Accepts<UpdateContributorRequest>("application/json")
      .Produces<UpdateContributorResponse>(200, "application/json")
      .ProducesProblem(404)
      .ProducesProblem(400));
  }

  public override async Task<Results<Ok<UpdateContributorResponse>, NotFound, ProblemHttpResult>> ExecuteAsync(UpdateContributorRequest request, CancellationToken ct)
  {
    var cmd = new UpdateContributorCommand(ContributorId.From(request.Id), ContributorName.From(request.Name!));
    var fin = await _mediator.Send(cmd, ct);

    if (fin.IsFail)
      return TypedResults.Problem(title: "Update failed", detail: fin.ToString(), statusCode: StatusCodes.Status400BadRequest);

    var opt = fin.Match(o => o, _ => Option<ContributorDto>.None);
    return opt.Match<Results<Ok<UpdateContributorResponse>, NotFound, ProblemHttpResult>>(
      v => TypedResults.Ok(Map.FromEntity(v)),
      () => TypedResults.NotFound());
  }
}

public sealed class UpdateContributorMapper : Mapper<UpdateContributorRequest, UpdateContributorResponse, ContributorDto>
{
  public override UpdateContributorResponse FromEntity(ContributorDto e) => new(new ContributorRecord(e.Id.Value, e.Name.Value, ""));
}
