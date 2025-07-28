using System.Dynamic;
using Microsoft.Data.Sqlite;

namespace Peeveen.Hotel.Ef.Test;

public sealed class SqliteTests : DatabaseTests, IDisposable {
	private const string DbFilePath = ".\\hotels_test.db";
	private readonly dynamic _configuration;

	protected override HotelContext CreateHotelContext() => new(
		SqliteDbContextOptionsConfigurator.Instance,
		_configuration
	);

	public SqliteTests() {
		// In case we bailed out of a debug run, delete the database file
		// that will still be hanging around.
		DeleteDatabase();
		dynamic configuration = new ExpandoObject();
		configuration.ConnectionString = $"Data Source={DbFilePath}";
		_configuration = configuration;
	}

	private static void DeleteDatabase() {
		if (File.Exists(DbFilePath))
			File.Delete(DbFilePath);
	}

	public void Dispose() {
		SqliteConnection.ClearAllPools();
		DeleteDatabase();
	}
}