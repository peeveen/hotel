using Microsoft.EntityFrameworkCore;

namespace Peeveen.Hotel.Ef;

public class HotelContext : DbContext {
	public DbSet<Hotel> Hotels { get; set; } = null!;
	public DbSet<Room> Rooms { get; set; } = null!;
	public DbSet<Booking> Bookings { get; set; } = null!;

	public HotelContext(DbContextOptions<HotelContext> options) : base(options) {
		Database.EnsureCreated();
	}

	// This is a constructor that allows the test to work.
	private readonly IDbContextOptionsConfigurator? _configurator;
	private readonly dynamic? _configuration;
	internal HotelContext(IDbContextOptionsConfigurator configurator, dynamic configuration) : base() {
		_configurator = configurator;
		_configuration = configuration;
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
		_configurator?.Configure(optionsBuilder, _configuration);

	protected override void OnModelCreating(ModelBuilder modelBuilder) =>
		modelBuilder.Entity<Hotel>()
			.HasMany(h => h.Rooms)
			.WithOne(r => r.Hotel);
}