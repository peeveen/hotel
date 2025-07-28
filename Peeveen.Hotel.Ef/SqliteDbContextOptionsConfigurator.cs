using Microsoft.EntityFrameworkCore;

namespace Peeveen.Hotel.Ef;

public class SqliteDbContextOptionsConfigurator : IDbContextOptionsConfigurator {
	private SqliteDbContextOptionsConfigurator() { }
	public static SqliteDbContextOptionsConfigurator Instance { get; } = new();

	public DbContextOptionsBuilder Configure(DbContextOptionsBuilder optionsBuilder, dynamic configuration) {
		// TODO: Validate configuration, maybe use AutoMapper to map to concrete type.
		string connectionString = configuration.ConnectionString;
		return optionsBuilder.UseSqlite(connectionString);
	}
}