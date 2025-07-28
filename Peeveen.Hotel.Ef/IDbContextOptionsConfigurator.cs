using Microsoft.EntityFrameworkCore;

namespace Peeveen.Hotel.Ef;

public interface IDbContextOptionsConfigurator {
	DbContextOptionsBuilder Configure(DbContextOptionsBuilder optionsBuilder, dynamic configuration);
}