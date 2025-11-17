# Clean Architecture Template – Reusable Snippets

## Table of Contents
1. Mediator Source Generator Registration
2. Mediator Pipeline – Logging Behavior
3. EF Core Fluent API – Vogen + Server-Assigned IDs
4. Offline-First IDs – Client PK + Server ID
5. SaveChanges Interceptor – Domain Event Dispatch
6. System.Text.Json Source Generation (AOT Friendly)
7. EF Tips for MAUI/AOT
8. No-Op Mediator for Unit Tests
9. Vogen EF Core Converters Partial Class
10. Data Schema Constants
11. Custom Vogen ID Value Generator (Reflection-Based)
12. List Contributors Query Service (Raw SQL + Paging)
13. Fake List Contributors Query Service (In-Memory)
14. Contributor GetById Endpoint
15. Infrastructure Service Registration
16. Create Contributor Endpoint
17. Delete Contributor Endpoint
18. List Contributors Endpoint
19. Update Contributor Endpoint
20. Contributor ById Specification
21. Vogen ID Value Generator (Reflection-Based)
22. DbContext Options with In-Memory DB and Interceptor
23. Db Context Registration Extension
24. Domain Event Dispatcher Implementation
25. EF Repository Implementation
26. Infrastructure Service Extensions

---

## 1. Mediator Source Generator Registration
```csharp
// Register Mediator with explicit assemblies and pipeline behaviors
services.AddMediator(options =>
{
  options.ServiceLifetime = ServiceLifetime.Scoped;

  options.Assemblies =
  [
    typeof(Contributor),                       // Core
    typeof(CreateContributorCommand),          // UseCases
    typeof(InfrastructureServiceExtensions),   // Infrastructure
    typeof(MediatorConfig)                     // Web
  ];

  // Order matters: Validation -> Logging -> Audit -> Caching (as needed)
  options.PipelineBehaviors =
  [
    typeof(LoggingBehavior<,>)
    // typeof(ValidationBehavior<,>),
    // typeof(AuditBehavior<,>),
    // typeof(CachingBehavior<,>)
  ];
});
```

## 2. Mediator Pipeline – Logging Behavior
```csharp
using System.Diagnostics;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IMessage
{
  private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

  public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

  public async ValueTask<TResponse> Handle(
    TRequest request,
    MessageHandlerDelegate<TRequest, TResponse> next,
    CancellationToken ct)
  {
    _logger.LogInformation("Handling {RequestName}: {@Request}", typeof(TRequest).Name, request);
    var sw = Stopwatch.StartNew();
    var response = await next(request, ct);
    _logger.LogInformation("Handled {RequestName} in {Ms} ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
    return response;
  }
}
```

## 3. EF Core Fluent API – Vogen + Server-Assigned IDs
Server supplies the IDs (e.g., from the backend). SQLite should never generate keys locally.
```csharp
public class ContributorConfiguration : IEntityTypeConfiguration<Contributor>
{
  public void Configure(EntityTypeBuilder<Contributor> builder)
  {
    builder.HasKey(e => e.Id);

    builder.Property(e => e.Id)
      .HasVogenConversion()   // Vogen <-> int
      .ValueGeneratedNever()  // server-assigned
      .IsRequired();

    builder.Property(e => e.Name)
      .HasVogenConversion()
      .HasMaxLength(ContributorName.MaxLength)
      .IsRequired();

    builder.OwnsOne(e => e.PhoneNumber);

    builder.Property(e => e.Status)
      .HasConversion(x => x.Value, x => ContributorStatus.FromValue(x));
  }
}
```
Alternative (DB-generated int keys, if you ever switch): replace `.ValueGeneratedNever()` with `.ValueGeneratedOnAdd()`.

## 4. Offline-First IDs – Client PK + Server ID
Use a client-stable Guid as the PK; set server ID after sync (keeps inserts working offline).
```csharp
// Entity (conceptual)
public class Contributor
{
  public Guid ClientId { get; private set; } = Guid.NewGuid(); // PK on client
  public ContributorId? ServerId { get; private set; }         // Vogen over int, nullable until synced

  // ... other fields
}

// Mapping
public class ContributorConfiguration : IEntityTypeConfiguration<Contributor>
{
  public void Configure(EntityTypeBuilder<Contributor> builder)
  {
    builder.HasKey(e => e.ClientId);

    builder.Property(e => e.ServerId)
      .HasVogenConversion()
      .ValueGeneratedNever()
      .IsRequired(false);

    builder.HasIndex(e => e.ServerId).IsUnique(); // unique when set; SQLite allows multiple NULLs
  }
}
```
Sync flow: send `ClientId` to the server as `ExternalId`, server upserts by `ExternalId`, returns `ServerId`. Client patches `ServerId` locally.

## 5. SaveChanges Interceptor – Domain Event Dispatch
```csharp
public sealed class EventDispatchInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
  private readonly IDomainEventDispatcher _dispatcher = dispatcher;

  public override async ValueTask<int> SavedChangesAsync(
    SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
  {
    if (eventData.Context is not AppDbContext ctx) return await base.SavedChangesAsync(eventData, result, ct);

    var entitiesWithEvents = ctx.ChangeTracker.Entries<HasDomainEventsBase>()
      .Select(e => e.Entity)
      .Where(e => e.DomainEvents.Any())
      .ToArray();

    await _dispatcher.DispatchAndClearEvents(entitiesWithEvents);
    return await base.SavedChangesAsync(eventData, result, ct);
  }
}
```
Register with DbContext options:
```csharp
options.AddInterceptors(sp.GetRequiredService<EventDispatchInterceptor>());
```

## 6. System.Text.Json Source Generation (AOT Friendly)
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

// DTO
public sealed record ContributorDto(int Id, string Name);

// Source-gen context
[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ContributorDto))]
public sealed partial class AppJsonContext : JsonSerializerContext { }

// Usage
var json = JsonSerializer.Serialize(dto, AppJsonContext.Default.ContributorDto);
var dto2 = JsonSerializer.Deserialize(json, AppJsonContext.Default.ContributorDto);
```

## 7. EF Tips for MAUI/AOT
- Disable lazy loading proxies; use Include/explicit loading.
- Prefer explicit configuration over scanning. Avoid `ApplyConfigurationsFromAssembly` on client.
- Consider EF compiled models once schema stabilizes.
- Keep value generators simple; avoid reflection-heavy generators on device.
- For server-assigned IDs, use `ValueGeneratedNever`.

## 8. No-Op Mediator for Unit Tests
```csharp
public class NoOpMediator : IMediator
{
  public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    => ValueTask.FromResult(default(TResponse)!);
  public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    => ValueTask.FromResult(default(TResponse)!);
  public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    => ValueTask.FromResult(default(TResponse)!);
  public ValueTask<object?> Send(object message, CancellationToken ct = default)
    => ValueTask.FromResult<object?>(null);

  public ValueTask Publish<TNotification>(TNotification notification, CancellationToken ct = default)
    where TNotification : INotification => ValueTask.CompletedTask;
  public ValueTask Publish(object notification, CancellationToken ct = default)
    => ValueTask.CompletedTask;

  public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)
    => AsyncEnumerable.Empty<TResponse>();
  public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken ct = default)
    => AsyncEnumerable.Empty<TResponse>();
  public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken ct = default)
    => AsyncEnumerable.Empty<TResponse>();
  public IAsyncEnumerable<object?> CreateStream(object message, CancellationToken ct = default)
    => AsyncEnumerable.Empty<object?>();
}
```

## 9. Vogen EF Core Converters Partial Class
```csharp
using Clean.Architecture.Core.ContributorAggregate;
using Vogen;

namespace Clean.Architecture.Infrastructure.Data.Config;

[EfCoreConverter<ContributorId>]
[EfCoreConverter<ContributorName>]
internal partial class VogenEfCoreConverters;
```

## 10. Data Schema Constants
```csharp
namespace Clean.Architecture.Infrastructure.Data.Config;

public static class DataSchemaConstants
{
  public const int DEFAULT_NAME_LENGTH = 100;
}
```

## 11. Custom Vogen ID Value Generator (Reflection-Based)
```csharp
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Clean.Architecture.Infrastructure.Data.Config;

internal class VogenIdValueGenerator<TContext, TEntityBase, TId> : ValueGenerator<TId>
    where TContext : DbContext
    where TEntityBase : EntityBase<TEntityBase, TId>
    where TId : IVogen<TId, int>
{
  private readonly PropertyInfo _matchPropertyGetter;

  public VogenIdValueGenerator()
  {
    var matchingProperties =
        typeof(TContext).GetProperties().Where(p => p!.GetGetMethod()!.IsPublic && p.PropertyType == typeof(DbSet<TEntityBase>)).ToList();

    if (matchingProperties.Count == 0)
    {
      throw new InvalidOperationException($"No properties found in the EFCore context for a DBSet of {nameof(TEntityBase)}");
    }

    if (matchingProperties.Count > 1)
    {
      throw new InvalidOperationException($"Multiple properties found in the EFCore context for a DBSet of {nameof(TEntityBase)}");
    }

    _matchPropertyGetter = matchingProperties[0];
  }

  public override TId Next(EntityEntry entry)
  {
    TContext ctx = (TContext)entry.Context;

    DbSet<TEntityBase> entities = (DbSet<TEntityBase>)_matchPropertyGetter!.GetValue(ctx)!;

    var next = Math.Max(
        MaxFrom(entities.Local),
        MaxFrom(entities)) + 1;

    return TId.From(next);

    static int MaxFrom(IEnumerable<TEntityBase> es) =>
        es.Any() ? es.Max(e => e.Id.Value) : 0;
  }

  public override bool GeneratesTemporaryValues => false;
}
```

## 12. List Contributors Query Service (Raw SQL + Paging)
```csharp
using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors;
using Clean.Architecture.UseCases.Contributors.List;

namespace Clean.Architecture.Infrastructure.Data.Queries;

public class ListContributorsQueryService : IListContributorsQueryService
{
  private readonly AppDbContext _db;
  public ListContributorsQueryService(AppDbContext db) => _db = db;

  public async Task<UseCases.PagedResult<ContributorDto>> ListAsync(int page, int perPage)
  {
    var items = await _db.Contributors.FromSqlRaw("SELECT Id, Name, PhoneNumber_CountryCode, PhoneNumber_Number, PhoneNumber_Extension FROM Contributors")
      .OrderBy(c => c.Id)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(c => new ContributorDto(c.Id, c.Name, c.PhoneNumber ?? PhoneNumber.Unknown))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await _db.Contributors.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);
    return new UseCases.PagedResult<ContributorDto>(items, page, perPage, totalCount, totalPages);
  }
}
```

## 13. Fake List Contributors Query Service (In-Memory)
```csharp
using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors;
using Clean.Architecture.UseCases.Contributors.List;

namespace Clean.Architecture.Infrastructure.Data.Queries;

public class FakeListContributorsQueryService : IListContributorsQueryService
{
  public Task<UseCases.PagedResult<ContributorDto>> ListAsync(int page, int perPage)
  {
    var items = new List<ContributorDto>();
    for (int i = 1; i <= 25; i++)
    {
      var phone = new PhoneNumber("+1", "555", "1234567");
      items.Add(new ContributorDto(ContributorId.From(i), ContributorName.From($"Fake {i}"), phone));
    }

    int totalPages = (int)Math.Ceiling(items.Count / (double)perPage);
    return Task.FromResult(new UseCases.PagedResult<ContributorDto>(items, page, perPage, items.Count, totalPages));
  }
}
```

## 14. Contributor GetById Endpoint
```csharp
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
```

## 15. Infrastructure Service Registration
```csharp
using Clean.Architecture.Core.Interfaces;
using Clean.Architecture.Core.Services;
using Clean.Architecture.Infrastructure.Data;
using Clean.Architecture.Infrastructure.Data.Queries;
using Clean.Architecture.UseCases.Contributors.List;

namespace Clean.Architecture.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("cleanarchitecture")
                               ?? config.GetConnectionString("DefaultConnection")
                               ?? config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      if (config.GetConnectionString("cleanarchitecture") != null ||
          config.GetConnectionString("DefaultConnection") != null)
      {
        options.UseSqlServer(connectionString);
      }
      else
      {
        throw new InvalidOperationException("Missing connection string: No valid database connection string found in configuration.");
      }

      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
           .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
           .AddScoped<IDeleteContributorService, DeleteContributorService>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
```

## 16. Create Contributor Endpoint
```csharp
using System.ComponentModel.DataAnnotations;
using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors.Create;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Clean.Architecture.Web.Contributors;

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
      .MaximumLength(ContributorName.MaxLength);
  }
}

public class CreateContributorResponse(int id, string name)
{
  public int Id { get; set; } = id;
  public string Name { get; set; } = name;
}
```

## 17. Delete Contributor Endpoint
```csharp
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
```

## 18. List Contributors Endpoint
```csharp
using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors;
using Clean.Architecture.UseCases.Contributors.List;
using FluentValidation;

namespace Clean.Architecture.Web.Contributors;

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
```

## 19. Update Contributor Endpoint
```csharp
using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors; // for ContributorDto
using Clean.Architecture.UseCases.Contributors.Update;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Clean.Architecture.Web.Contributors;

public class Update(IMediator mediator)
  : Endpoint<UpdateContributorRequest, Results<Ok<UpdateContributorResponse>, NotFound, ProblemHttpResult>, UpdateContributorMapper>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Put(UpdateContributorRequest.Route);
    AllowAnonymous();

    Summary(s =>
    {
      s.Summary = "Update a contributor";
      s.Description = "Updates an existing contributor's information.";
      s.ExampleRequest = new UpdateContributorRequest { Id = 1, Name = "Updated Name" };
      s.ResponseExamples[200] = new UpdateContributorResponse(new ContributorRecord(1, "Updated Name", ""));
      s.Responses[200] = "Contributor updated successfully";
      s.Responses[404] = "Contributor with specified ID not found";
      s.Responses[400] = "Invalid input data";
    });

    Tags("Contributors");

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
```

## 20. Contributor ById Specification
```csharp
public class ContributorByIdSpec : Specification<Contributor>
{
  public ContributorByIdSpec(ContributorId contributorId) =>
    Query
        .Where(contributor => contributor.Id == contributorId);
}
```

## 21. Vogen ID Value Generator (Reflection-Based)
```csharp
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Clean.Architecture.Infrastructure.Data.Config;

internal class VogenIdValueGenerator<TContext, TEntityBase, TId> : ValueGenerator<TId>
    where TContext : DbContext
    where TEntityBase : EntityBase<TEntityBase, TId>
    where TId : IVogen<TId, int>
{
  private readonly PropertyInfo _matchPropertyGetter;

  public VogenIdValueGenerator()
  {
    var matchingProperties =
        typeof(TContext).GetProperties().Where(p => p!.GetGetMethod()!.IsPublic && p.PropertyType == typeof(DbSet<TEntityBase>)).ToList();

    if (matchingProperties.Count == 0)
    {
      throw new InvalidOperationException($"No properties found in the EFCore context for a DBSet of {nameof(TEntityBase)}");
    }

    if (matchingProperties.Count > 1)
    {
      throw new InvalidOperationException($"Multiple properties found in the EFCore context for a DBSet of {nameof(TEntityBase)}");
    }

    _matchPropertyGetter = matchingProperties[0];
  }

  public override TId Next(EntityEntry entry)
  {
    TContext ctx = (TContext)entry.Context;

    DbSet<TEntityBase> entities = (DbSet<TEntityBase>)_matchPropertyGetter!.GetValue(ctx)!;

    var next = Math.Max(
        MaxFrom(entities.Local),
        MaxFrom(entities)) + 1;

    return TId.From(next);

    static int MaxFrom(IEnumerable<TEntityBase> es) =>
        es.Any() ? es.Max(e => e.Id.Value) : 0;
  }

  public override bool GeneratesTemporaryValues => false;
}
```

## 22. DbContext Options with In-Memory DB and Interceptor
```csharp
using Clean.Architecture.Core.ContributorAggregate;

namespace Clean.Architecture.Infrastructure.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Contributor> Contributors => Set<Contributor>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}
```

## 23. Db Context Registration Extension
```csharp
public static class AppDbContextExtensions
{
  public static void AddApplicationDbContext(this IServiceCollection services, string connectionString) =>
    services.AddDbContext<AppDbContext>(options =>
         options.UseSqlServer(connectionString));
}
```

## 24. Domain Event Dispatcher Implementation
```csharp
// Intercepts SaveChanges to dispatch domain events after changes are successfully saved
public class EventDispatchInterceptor(IDomainEventDispatcher domainEventDispatcher) : SaveChangesInterceptor
{
  private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;

  // Called after SaveChangesAsync has completed successfully
  public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
    CancellationToken cancellationToken = new CancellationToken())
  {
    var context = eventData.Context;
    if (context is not AppDbContext appDbContext)
    {
      return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    // Retrieve all tracked entities that have domain events
    var entitiesWithEvents = appDbContext.ChangeTracker.Entries<HasDomainEventsBase>()
      .Select(e => e.Entity)
      .Where(e => e.DomainEvents.Any())
      .ToArray();

    // Dispatch and clear domain events
    await _domainEventDispatcher.DispatchAndClearEvents(entitiesWithEvents);

    return await base.SavedChangesAsync(eventData, result, cancellationToken);
  }
}
```

## 25. EF Repository Implementation
```csharp
// inherit from Ardalis.Specification type
public class EfRepository<T>(AppDbContext dbContext) :
  RepositoryBase<T>(dbContext), IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
{
}
```

## 26. Infrastructure Service Extensions
```csharp
public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    // Try to get connection strings in order of priority:
    // 1. "cleanarchitecture" - provided by Aspire when using .WithReference(cleanArchDb)
    // 2. "DefaultConnection" - traditional SQL Server connection
    // 3. "SqliteConnection" - fallback to SQLite
    string? connectionString = config.GetConnectionString("cleanarchitecture")
                               ?? config.GetConnectionString("DefaultConnection")
                               ?? config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      // Use SQL Server if Aspire or DefaultConnection is available, otherwise use SQLite
      if (config.GetConnectionString("cleanarchitecture") != null ||
          config.GetConnectionString("DefaultConnection") != null)
      {
        options.UseSqlServer(connectionString);
      }
      else
      {
        throw new InvalidOperationException("Missing connection string: No valid database connection string found in configuration.");
      }

      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
           .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
           .AddScoped<IDeleteContributorService, DeleteContributorService>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
