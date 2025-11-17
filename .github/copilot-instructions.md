# GitHub Copilot Instructions for <Your Project Name>

## Project Overview
This is a **Clean Architecture template** for .NET 10 that implements Specification Driven Design (DDD) patterns. It uses FastEndpoints for API development, MediatR for CQRS, and EF Core for data access. The architecture is layered to ensure separation of concerns and maintainability.
## Architecture & Project Structure

### C# Conventions
- Use standard Microsoft naming conventions
- Use `PascalCase` for types and methods, `camelCase` for parameters and private fields
- Use `I` prefix for interfaces (e.g., `IRepository`)
- Use `Async` suffix for async methods (e.g., `GetByIdAsync`)
- Prefix private fields with `_` (e.g., `_repository`)
- Always use {} for blocks except single-line exits (e.g. `return`, `throw`)
- Always keep single line blocks on one line (e.g., `if (x) return y;`)
- Prefer primary constructors for required dependencies

### Core Dependencies Flow
- **Core** ← UseCases ← Infrastructure 
- **Core** ← UseCases ← Web
- Never allow Core to depend on outer layers

### Key Projects
- **Core**: Domain entities, aggregates, value objects, specifications, interfaces
- **UseCases**: Commands/queries (CQRS), MediatR handlers, application logic  
- **Infrastructure**: EF Core, external services, email, file access
- **Web**: FastEndpoints API, REPR pattern, validation

## Development Patterns

## Specifications
- Specifications are expressed in Yaml prompts. 
- They have a schema defined in prompt-spec.schema.yaml.
- They are stored in the `YamlPrompts` folder.
- They defined the behavior for the solution. A behavior usually defines a measureable and discrete piece of functionality.
- Each behavior is implemented in code as a set of classes and methods that fulfill the requirements and its associated tests. It's evaluated by the Acceptance Criteria.
- Specifications can be composed of other specifications to build complex behaviors from simpler ones. This composability is a key feature of the specification pattern.
- For aspects of the specification that contains complicated logic, we use BDD style Gherkin files to define the scenarios and expected outcomes. This comes with the benefit of thorough tests and documentation.

### API Endpoints (FastEndpoints + REPR)
- One endpoint per file: `Create.cs`, `Update.cs`, `Delete.cs`, `GetById.cs`
- Separate request/response/validator files: `Create.CreateRequest.cs`, `Create.CreateValidator.cs`
- Use `Endpoint<TRequest, TResponse>` base class
- Example: `src/Clean.Architecture.Web/Contributors/Create.cs`

### Domain Model (Core)
- Entities use encapsulation - minimize public setters
- Group related entities into Aggregates
- Use Value Objects (e.g., `ContributorName.From()`)
- Domain Events for cross-aggregate communication
- Repository interfaces defined in Core, implemented in Infrastructure

### Use Cases (CQRS)
- Commands for mutations, Queries for reads
- Queries can bypass repository pattern for performance
- Use MediatR for command/query handling
- Chain of responsibility for cross-cutting concerns (logging, validation)

### Validation Strategy
- **API Level**: FluentValidation on request DTOs (FastEndpoints integration)
- **Use Case Level**: Validate commands/queries (defensive coding)
- **Domain Level**: Business invariants throw exceptions, assume pre-validated input

## Essential Commands

### Build & Test
```bash
dotnet build Clean.Architecture.slnx
dotnet test Clean.Architecture.slnx
```

### Entity Framework Migrations
```bash
# From Web project directory
dotnet ef migrations add MigrationName -c AppDbContext -p ../Clean.Architecture.Infrastructure/Clean.Architecture.Infrastructure.csproj -s Clean.Architecture.Web.csproj -o Data/Migrations

dotnet ef database update -c AppDbContext -p ../Clean.Architecture.Infrastructure/Clean.Architecture.Infrastructure.csproj -s Clean.Architecture.Web.csproj
```

### Template Installation & Usage
```bash
dotnet new install Ardalis.CleanArchitecture.Template
dotnet new clean-arch -o Your.ProjectName
```

## Key Dependencies & Patterns

### Primary Libraries
- **FastEndpoints**: API endpoints (replaced Controllers/Minimal APIs)
- **MediatR**: Command/query handling in UseCases
- **EF Core**: Data access (SQLite default, easily changed to SQL Server)
- **Ardalis.Specification**: Repository query specifications
- **Ardalis.Result**: Error handling pattern
- **Serilog**: Structured logging

### Central Package Management
- All package versions in `Directory.Packages.props`
- Use `<PackageReference Include="..." />` without Version attribute

### Test Organization
- **UnitTests**: Core business logic, use cases
- **IntegrationTests**: Database, infrastructure components  
- **FunctionalTests**: API endpoints (subcutaneous testing)
- Use `Microsoft.AspNetCore.Mvc.Testing` for API tests

## File Organization Conventions

### Web Project Structure
```
Contributors/
  Create.cs                    # Endpoint
  Create.CreateRequest.cs      # Request DTO
  Create.CreateResponse.cs     # Response DTO  
  Create.CreateValidator.cs    # FluentValidation
  Update.cs, Delete.cs, etc.
```

### Sample vs Template
- `/sample` folder: Complete working example (NimblePros.SampleToDo)
- `/src` folder: Clean template ready for your project
- Study sample for patterns, use src for new projects

## Common Gotchas

- Don't include hyphens in project names (template limitation)
- Attach the ZoEazy.Shared solution for shared classes, utilities, etc.
- Database path in `appsettings.json` forSQL server.
- Use absolute paths in EF migration commands
- FastEndpoints uses different validation approach than Controller-based APIs

## Specifications
Refer to these documents for detailed specifications on other aspects of the project:
- [Constitution](constitution.md)
- [Architecture](architecture.md)
- [Best Practices](best-practices.md)
- [Naming Conventions](naming-conventions.md)
- [Performance Considerations](performance.md)
- [TDD (Reasonable TDD)](reasonable-tdd.md)
- [Repository Rules](repository-rules.md) 
- [Security Considerations](security.md)
- [Testing](testing.md)

- [Snippets (not standardized and not curated, be careful)]