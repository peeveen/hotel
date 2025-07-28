using FluentValidation;

namespace Peeveen.Hotel.Webservice;

public class ServiceOptions {
	public required string DatabaseType { get; init; }
	public required string ConnectionString { get; init; }
}

public class ServiceOptionsValidator : AbstractValidator<ServiceOptions> {
	public ServiceOptionsValidator() {
		RuleFor(c => c.DatabaseType).NotEmpty();
		RuleFor(c => c.ConnectionString).NotEmpty();
	}
}