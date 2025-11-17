using Core.ContributorAggregate;

namespace UseCases.Contributors;

public record ContributorDto(ContributorId Id, ContributorName Name, PhoneNumber PhoneNumber);
