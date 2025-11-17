# Best Practices for .NET 10 Development

## General Coding Practices

### 1. Use Modern C# Features and LanguageExt for functional features.
### Where possible, use Fin<T> because it's a clear option between success and failure.

#### Nullable Reference Types
```csharp
// Enable in project file
<Nullable>enable</Nullable>

// Use nullable annotations
public string? OptionalValue { get; set; }
public string RequiredValue { get; set; } = string.Empty;
```

#### File-Scoped Namespaces
```csharp
namespace MyApp.Domain.Models;

using LanguageExt;
using static LanguageExt.Prelude;

public class User
{
    // Class implementation
}
```

#### Record Types for DTOs
```csharp
using LanguageExt;

public record UserDto(int Id, string Name, string Email);
public record CreateUserRequest(string Name, string Email);
```

#### Pattern Matching
```csharp
public string GetUserType(User user) => user switch
{
    { IsAdmin: true } => "Administrator",
    { IsPremium: true } => "Premium User",
    _ => "Regular User"
};
```

### 2. Asynchronous Programming

#### Always Use Async/Await Properly
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

// ✅ Good
public async Task<Fin<User>> GetUserAsync(int id)
{
    try
    {
        var user = await _context.Users.FindAsync(id);
        return user is not null ? Success<Fin<User>>(user) : Fail<User>(Error.New($"User {id} not found"));
    }
    catch (Exception ex)
    {
        return Fail<User>(Error.New(ex.Message));
    }
}

// ❌ Bad - async void (except event handlers)
public async void ProcessUser(int id) { }

// ❌ Bad - blocking on async
public User GetUser(int id)
{
    return _context.Users.FindAsync(id).Result; // Deadlock risk
}
```

#### Use ConfigureAwait When Appropriate
```csharp
var data = await _httpClient.GetAsync(url).ConfigureAwait(false);
```

### 3. Dependency Injection

#### Constructor Injection (Preferred)
```csharp
using LanguageExt;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Fin<User>> GetUserAsync(int id)
    {
        var result = await _userRepository.GetByIdAsync(id);
        return result;
    }
}
```

#### Service Registration
```csharp
// Program.cs
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddTransient<IEmailService, EmailService>();
```

### 4. Error Handling

#### Use Specific Exceptions
```csharp
using LanguageExt;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(int userId) 
        : base($"User with ID {userId} was not found") { }
}

// Use custom exceptions with Fin
public async Task<Fin<User>> GetUserAsync(int userId)
{
    var user = await _context.Users.FindAsync(userId);
    return user is not null
        ? Success<Fin<User>>(user)
        : Fail<User>(Error.New(new UserNotFoundException(userId)));
}
```

#### Global Exception Handling
```csharp
// Program.cs
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception");
            await context.Response.WriteAsJsonAsync(new { error = "An error occurred" });
        }
    });
});
```

### 5. Logging

#### Structured Logging
```csharp
// ✅ Good - structured
_logger.LogInformation("User {UserId} performed action {Action}", userId, action);

// ❌ Bad - string interpolation
_logger.LogInformation($"User {userId} performed action {action}");
```

#### Log Levels
- **Trace**: Very detailed debugging
- **Debug**: Development debugging
- **Information**: General flow
- **Warning**: Abnormal but expected
- **Error**: Errors and exceptions
- **Critical**: Fatal errors

### 6. Entity Framework Core

#### Use Async Methods
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

var users = await TryAsync(_context.Users.Where(u => u.IsActive).ToListAsync());
```

#### Avoid N+1 Queries
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

// ✅ Good - eager loading
var users = await TryAsync(_context.Users.Include(u => u.Orders).ToListAsync());

// ❌ Bad - causes N+1 queries
var users = await _context.Users.ToListAsync();
foreach (var user in users)
{
    var orders = await _context.Orders.Where(o => o.UserId == user.Id).ToListAsync();
}
```

#### Use AsNoTracking for Read-Only Queries
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

var users = await TryAsync(_context.Users.AsNoTracking().ToListAsync());
```

#### Database Transactions
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await TryAsync(_context.Users.AddAsync(user));
    await TryAsync(_context.SaveChangesAsync());
    await TryAsync(_context.Orders.AddAsync(order));
    await TryAsync(_context.SaveChangesAsync());
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 7. API Design

#### Use Proper HTTP Status Codes
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(int id)
{
    var result = await _userService.GetUserAsync(id);
    return result.Match(
        Succ: user => Ok(user),
        Fail: err => NotFound(err.Message)
    );
}

[HttpPost]
public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
{
    var result = await _userService.CreateUserAsync(request);
    return result.Match(
        Succ: user => CreatedAtAction(nameof(GetUser), new { id = user.Id }, user),
        Fail: err => BadRequest(err.Message)
    );
}
```

#### API Versioning
```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Controller
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
}
```

### 8. Validation

#### Use FluentValidation
```csharp
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

### 9. Configuration

#### Use Options Pattern
```csharp
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string FromAddress { get; set; } = string.Empty;
}

// Program.cs
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Usage
public class EmailService
{
    private readonly EmailSettings _settings;
    
    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

### 10. Performance

#### Use Span<T> and Memory<T>
```csharp
public void ProcessData(ReadOnlySpan<byte> data)
{
    // Efficient memory operations
}
```

#### String Interpolation vs String.Format
```csharp
// ✅ Good - use interpolation for simple cases
var message = $"Hello, {name}!";

// Use StringBuilder for loops
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.AppendLine($"Item: {item}");
}
```

#### Cache Appropriately
```csharp
using LanguageExt;
using static LanguageExt.Prelude;

public class CacheService
{
    private readonly IMemoryCache _cache;
    public async Task<Fin<T>> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
    {
        try
        {
            if (!_cache.TryGetValue(key, out T value))
            {
                value = await factory();
                _cache.Set(key, value, TimeSpan.FromMinutes(10));
            }
            return Success<Fin<T>>(value);
        }
        catch (Exception ex)
        {
            return Fail<T>(Error.New(ex.Message));
        }
    }
}
```

## Testing Best Practices

### Unit Tests
```csharp
using NUnit.Framework;
using FluentAssertions;
using Moq;
using LanguageExt;
using static LanguageExt.Prelude;

[TestFixture]
public class UserServiceTests
{
    [Test]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new User { Id = userId, Name = "Test" };
        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(Success<Fin<User>>(expectedUser));
        var userService = new UserService(mockRepository.Object);
        // Act
        var result = await userService.GetUserAsync(userId);
        // Assert
        result.Match(
            Succ: user => {
                user.Should().NotBeNull();
                user.Id.Should().Be(userId);
            },
            Fail: err => Assert.Fail(err.Message)
        );
    }
}
```

### Integration Tests
```csharp
using NUnit.Framework;
using System.Net;
using LanguageExt;
using static LanguageExt.Prelude;

[TestFixture]
public class UserApiTests
{
    private HttpClient _client;
    [SetUp]
    public void Setup()
    {
        // Setup _client with test server
    }
    [Test]
    public async Task GetUsers_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/v1/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Security Best Practices

### Input Validation
```csharp
// Always validate and sanitize
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    // Additional validation
    if (await _userService.EmailExistsAsync(request.Email))
        return Conflict("Email already exists");
    
    // Process...
}
```

### Authentication & Authorization
```csharp
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(int id)
{
    await _userService.DeleteUserAsync(id);
    return NoContent();
}
```

### Use HTTPS
```csharp
// Program.cs
app.UseHsts();
app.UseHttpsRedirection();
```

## Code Organization

### Follow Clean Architecture
```
src/
├── Domain/           # Entities, value objects, domain events
├── Application/      # Use cases, DTOs, interfaces
├── Infrastructure/   # Data access, external services
└── Presentation/     # API controllers, views
```

### Use Minimal APIs for Simple Endpoints
```csharp
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/users/{id}", async (int id, IUserService service) =>
{
    var user = await service.GetUserAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});
```
