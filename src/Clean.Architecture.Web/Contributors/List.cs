using Core.ContributorAggregate;

using FluentValidation;

using UseCases.Contributors;
using UseCases.Contributors.List;

namespace Web.Contributors;

public class List(IMediator mediator) : Endpoint<ListContributorsRequest, ContributorListResponse, ListContributorsMapper>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/Contributors");
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "List contributors with pagination";
      s.Description = "Retrieves a paginated list of all contributors.";
      s.ExampleRequest = new ListContributorsRequest { Page = 1, PerPage = 10 };
      s.ResponseExamples[200] = new ContributorListResponse(
        new List<ContributorRecord>
        {
          new(1, "John Doe", PhoneNumber.Unknown.ToString()),
          new(2, "Jane Smith", PhoneNumber.Unknown.ToString())
        },
        1, 10, 2, 1);
      s.Params["page"] = "1-based page index (default 1)";
      s.Params["per_page"] = $"Page size 1–{UseCases.Constants.MAX_PAGE_SIZE} (default {UseCases.Constants.DEFAULT_PAGE_SIZE})";
      s.Responses[200] = "Paginated list of contributors";
      s.Responses[400] = "Invalid pagination parameters";
    });
    Tags("Contributors");
    Description(builder => builder
      .Accepts<ListContributorsRequest>()
      .Produces<ContributorListResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListContributorsRequest request, CancellationToken cancellationToken)
  {
    var fin = await _mediator.Send(new ListContributorsQuery(request.Page, request.PerPage));

    if (fin.IsFail)
    {
      AddError(fin.ToString());
      await Send.ErrorsAsync(statusCode: 400, cancellation: cancellationToken);
      return;
    }

    var paged = fin.Match(p => p, _ => default!); // safe since IsFail checked
    AddLinkHeader(paged.Page, paged.PerPage, paged.TotalPages);
    var response = Map.FromEntity(paged);
    await Send.OkAsync(response, cancellationToken);
  }

  private void AddLinkHeader(int page, int perPage, int totalPages)
  {
    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
    string Link(string rel, int p) => $"<{baseUrl}?page={p}&per_page={perPage}>; rel=\"{rel}\"";

    var parts = new List<string>();
    if (page > 1)
    {
      parts.Add(Link("first", 1));
      parts.Add(Link("prev", page - 1));
    }
    if (page < totalPages)
    {
      parts.Add(Link("next", page + 1));
      parts.Add(Link("last", totalPages));
    }

    if (parts.Count > 0)
      HttpContext.Response.Headers["Link"] = string.Join(", ", parts);
  }
}

public sealed class ListContributorsRequest
{
  [BindFrom("page")] public int Page { get; init; } = 1;
  [BindFrom("per_page")] public int PerPage { get; init; } = UseCases.Constants.DEFAULT_PAGE_SIZE;
}

public record ContributorListResponse : UseCases.PagedResult<ContributorRecord>
{
  public ContributorListResponse(IReadOnlyList<ContributorRecord> Items, int Page, int PerPage, int TotalCount, int TotalPages)
    : base(Items, Page, PerPage, TotalCount, TotalPages) { }
}

public sealed class ListContributorsValidator : Validator<ListContributorsRequest>
{
  public ListContributorsValidator()
  {
    RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page must be >= 1");
    RuleFor(x => x.PerPage).InclusiveBetween(1, UseCases.Constants.MAX_PAGE_SIZE).WithMessage($"per_page must be between 1 and {UseCases.Constants.MAX_PAGE_SIZE}");
  }
}

public sealed class ListContributorsMapper
  : Mapper<ListContributorsRequest, ContributorListResponse, UseCases.PagedResult<ContributorDto>>
{
  public override ContributorListResponse FromEntity(UseCases.PagedResult<ContributorDto> e)
  {
    var items = e.Items.Select(c => new ContributorRecord(c.Id.Value, c.Name.Value, c.PhoneNumber.ToString())).ToList();
    return new ContributorListResponse(items, e.Page, e.PerPage, e.TotalCount, e.TotalPages);
  }
}
