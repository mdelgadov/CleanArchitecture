using System.ComponentModel.DataAnnotations;

using FluentValidation;

using Microsoft.AspNetCore.Http.HttpResults;

using UseCases.Contributors.Create;

using ContributorName = Core.ContributorAggregate.ContributorName;

namespace Web.Contributors;

public class Create(IMediator mediator)
  : Endpoint<CreateContributorRequest, Results<Created<CreateContributorResponse>, ProblemHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post(CreateContributorRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Create a new contributor";
      s.Description = "Creates a new contributor.";
      s.ExampleRequest = new CreateContributorRequest { Name = "John Doe" };
      s.ResponseExamples[201] = new CreateContributorResponse(1, "John Doe");
      s.Responses[201] = "Contributor created successfully";
      s.Responses[400] = "Invalid input data";
    });
    Tags("Contributors");
    Description(builder => builder
      .Accepts<CreateContributorRequest>("application/json")
      .Produces<CreateContributorResponse>(201, "application/json")
      .ProducesProblem(400));
  }

  public override async Task<Results<Created<CreateContributorResponse>, ProblemHttpResult>> ExecuteAsync(CreateContributorRequest request, CancellationToken cancellationToken)
  {
    var fin = await _mediator.Send(new CreateContributorCommand(ContributorName.From(request.Name!), request.PhoneNumber));
    return fin.Match<Results<Created<CreateContributorResponse>, ProblemHttpResult>>(
      id => TypedResults.Created($"/Contributors/{id}", new CreateContributorResponse(id.Value, request.Name!)),
      err => TypedResults.Problem(title: "Create failed", detail: err.ToString(), statusCode: StatusCodes.Status400BadRequest));
  }
}

public class CreateContributorRequest
{
  public const string Route = "/Contributors";

  [Required]
  public string Name { get; set; } = string.Empty;
  public string? PhoneNumber { get; set; } = null;
}

public class CreateContributorValidator : Validator<CreateContributorRequest>
{
  public CreateContributorValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Name is required.")
      .MinimumLength(2)
      .MaximumLength(Core.ContributorAggregate.ContributorName.MaxLength);
  }
}

public class CreateContributorResponse(int id, string name)
{
  public int Id { get; set; } = id;
  public string Name { get; set; } = name;
}
