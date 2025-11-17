# [PROJECT_NAME] Constitution

## Constitution Version: [VERSION]

## Core Principles

This template establishes the foundational principles for building robust, maintainable, and scalable .NET web applications.

### 1. Code Quality First

- **Maintainability**: Write code that is easy to understand, modify, and extend
- **Readability**: Prioritize clear, self-documenting code over clever solutions
- **Testability**: Design with testing in mind from the start
- **Simplicity**: Follow KISS (Keep It Simple, Stupid) principle

### 2. Performance & Scalability

- Design for horizontal scalability
- Optimize database queries and API calls
- Implement caching strategies appropriately
- Monitor and measure performance metrics

### 3. Security by Design

- Never trust user input - validate and sanitize everything
- Implement proper authentication and authorization
- Follow OWASP Top 10 security guidelines
- Keep dependencies up to date
- Use secrets management (never commit secrets)

### 4. Modern Development Practices

- Follow Domain-Driven Design (DDD) principles
- Apply SOLID principles consistently
- Use dependency injection throughout
- Implement proper error handling and logging
- Version all APIs

### 5. Team Collaboration

- Write comprehensive documentation
- Conduct thorough code reviews
- Follow consistent naming conventions
- Maintain backward compatibility when possible
- Communicate changes effectively

### 6. Continuous Improvement

- Refactor continuously
- Update dependencies regularly
- Monitor and act on metrics
- Learn from production issues
- Stay current with .NET ecosystem

### 7. CDD, SDD, BDD and TDD... reasonable TDD
- CDD: Collaboration d.d. means, agents and developers interact and iterate in small pieces to resolve the big challenges.
- SDD: We use specs as the first documentation, the first source of the truth, the path for developers and agents to reach results. Spec Kit and our own spec implementations get us there.
- BDD: We think that for code that requires extra care, specifications lack strict detailed approach. Open language and agents only can take us so far. BDD using ReqNRoll and Gherkin allows a detailed approach and testability through integration testing.
- Reasonable TDD: Read the document on Reasonable TDD, but in a nutshell, we test for the system and not the system for the tests.

### Railroad-Oriented Programming
- Along LanguageExt and natural C# functionality and especially for heavy logic, we implement Railroad-oriented programming (RROP) as a useful approach for streamlining logic and isolating execution paths, which can enhance testability and maintainability.
- We make the caveat, however, that is not a silver bullet. While RRP emphasizes the “happy path,” not all alternative routes necessarily lead to failure. Some may represent valid alternative successes. It is important to thoughtfully consider and handle these alternative outcomes in your design, rather than ignoring them for the sake of linearity.
- We reckon three kinds of paths: "Happies", Errors and Exceptions. No Merges, even if a bit larger code is created, it pays for its simplicity.
- A good strategy is to prepare the ReqNRoll features with a test for each route, this guarantees 100% coverage without second guessing and ambiguity.

## Technology Stack Preferences

### Backend

- **.NET 10**: Latest LTS version
- **ASP.NET Core**: Web framework
- **Entity Framework Core**: ORM
- **Mediator**: CQRS pattern implementation
- **FluentValidation**: Input validation

### Database

- **PostgreSQL** or **SQL Server**: Primary database
- **Redis**: Caching and session storage

### DevOps

- **Docker**: Containerization
- **Azure Pipelines**: CI/CD
- **Azure**: Cloud hosting

## Project Structure Philosophy

Every project should follow a clean architecture pattern:

1. **Host Layer**: Controllers, Views, API endpoints
2. **Features Layer**: Business logic, use cases, DTOs
3. **Domain Layer**: Entities, value objects, domain events
4. **Infrastructure Layer**: Data access, external services, file system

## Quality Gates

All code must pass these gates before merging:

1. ✅ All unit tests pass
2. ✅ Code coverage > 80%
3. ✅ No critical security vulnerabilities
4. ✅ Linting and formatting checks pass
5. ✅ Peer review approval
6. ✅ Integration tests pass
7. ✅ Performance benchmarks met

## Change Management

## Related Standards and Supporting Documents

To ensure consistency and quality across all .NET projects, the following documents are considered authoritative and must be followed in addition to this constitution:

- [Development Rules](rules.md): Detailed code review, workflow, testing, security, and deployment requirements.
- [Naming Conventions](naming-conventions.md): Standards for naming code elements, files, APIs, and database objects.
- [Best Practices](best-practices.md): Recommended patterns, modern C# features, error handling, logging, and architecture guidance.

## Specifications

Also, use the following documents to align to the styles and options in your code.

- [Performance](performance.md): Performace indicators criteria and detailed sample of performance optimized common patterns.
- [Security](security.md): Detailed sample of JWT creation.
- [Testing](testing.md): Test coverage criteria and detailed sample of testing optimized common patterns.
- [Reasonable TDD](reasonable-tdd.md): TDD with practical and tamed accents.

All contributors and reviewers must consult these documents during development, code review, and onboarding. Automated tools and CI/CD pipelines should enforce compliance with these standards.
