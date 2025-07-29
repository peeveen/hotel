using System.Dynamic;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Opw.HttpExceptions.AspNetCore;
using Peeveen.Hotel;
using Peeveen.Hotel.Ef;
using Peeveen.Hotel.Webservice;

const string ServiceOptionsConfigurationSectionName = "Service";
const string AppSettingsFilename = "appsettings";

var builder = WebApplication.CreateBuilder(args);
string currentFolder = $".{Path.DirectorySeparatorChar}";
builder.Configuration.AddYamlFile(Path.Combine(currentFolder, $"{AppSettingsFilename}.yml"));
var environmentName = builder.Environment.EnvironmentName;
if (!string.IsNullOrEmpty(environmentName))
	builder.Configuration.AddYamlFile(Path.Combine(currentFolder, $"{AppSettingsFilename}.{environmentName}.yml"), optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);
var serviceConfigSection = builder.Configuration.GetRequiredSection(ServiceOptionsConfigurationSectionName);

Type[] unserializableTypes = [typeof(Type), typeof(PropertyInfo), typeof(FieldInfo)];
void IgnoreUnserializableProperties(JsonTypeInfo ti) {
	if (ti.Kind != JsonTypeInfoKind.Object) return;
	for (int f = ti.Properties.Count - 1; f >= 0; --f)
		if (unserializableTypes.Contains(ti.Properties[f].PropertyType))
			ti.Properties.RemoveAt(f);
}

var services = builder.Services;
services
	.AddScoped<HotelApi>()
	.AddScoped<IHotelStore, EfHotelStore>()
	.AddDbContext<HotelContext>((services, options) => {
		var serviceOptions = services.GetRequiredService<IOptions<ServiceOptions>>().Value;
		var databaseType = serviceOptions.DatabaseType;
		var configurator = services.GetRequiredKeyedService<IDbContextOptionsConfigurator>(databaseType);

		dynamic dynamicConfiguration = new ExpandoObject();
		dynamicConfiguration.ConnectionString = serviceOptions.ConnectionString;

		configurator.Configure(options, dynamicConfiguration);
	})
	.AddKeyedSingleton<IDbContextOptionsConfigurator>("sqlite", SqliteDbContextOptionsConfigurator.Instance)
	.AddEndpointsApiExplorer()
	.AddSwaggerGen();
services.AddOptions<ServiceOptions>().Bind(serviceConfigSection).ValidateOnStart();
services.AddMvc().AddHttpExceptions(options =>
	options.IncludeExceptionDetails = _ => true
).AddJsonOptions(options => {
	options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
	options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver {
		Modifiers = { IgnoreUnserializableProperties }
	};
});

var app = builder.Build();
app
	.UseSwagger()
	.UseSwaggerUI()
	.UseHttpExceptions();

static OpenApiString EncodeExample<T>(T example) => new(JsonSerializer.Serialize(example));

var createHotelExample = new CreateHotelRequest(
	"The Overlook Hotel",
	new Dictionary<uint, uint> {
		{ 101, 2 },
		{ 102, 3 },
		{ 103, 1 },
		{ 201, 2 },
		{ 202, 1 },
		{ 203, 5 }
	}
);

app.MapPost("/createHotel", (
	[FromBody] CreateHotelRequest addHotelRequest,
	[FromServices] HotelApi hotelApi
) => hotelApi.CreateHotelAsync(
	addHotelRequest.Name,
	addHotelRequest.Rooms
)).WithOpenApi(op => {
	op.OperationId = "CreateHotel";
	op.Description = "Creates a new hotel record.";
	op.RequestBody.Description = "JSON describing the hotel to create, including its name and rooms.";
	op.RequestBody.Content[MediaTypeNames.Application.Json].Example = EncodeExample(createHotelExample);
	return op;
})
.Accepts<CreateHotelRequest>(MediaTypeNames.Application.Json)
.Produces<ulong>((int)HttpStatusCode.OK, MediaTypeNames.Text.Plain);

app.MapGet("/getHotel/{nameOrPartOfName}", (
	[FromRoute] string nameOrPartOfName,
	[FromServices] HotelApi hotelApi
) => hotelApi.GetHotelAsync(
	nameOrPartOfName
)).WithOpenApi(op => {
	op.OperationId = "GetHotel";
	op.Description = "Finds hotels with names that contain the given search text (case-insensitive).";
	op.Parameters[0].Description = $"The name (or part of the name) of the hotel to search for (must be at least {HotelApi.MinimumSearchStringLength} characters).";
	return op;
})
.Produces<IHotel[]>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

var today = DateTime.UtcNow.Date;
var getAvailableRoomsExample = new GetAvailableRoomsRequest(1, 3, today.AddDays(1), today.AddDays(10));
app.MapPost("/getAvailableRooms", (
	[FromBody] GetAvailableRoomsRequest request,
	[FromServices] HotelApi hotelApi
) => hotelApi.GetAvailableRoomsAsync(
	request.HotelId,
	request.MinCapacity,
	request.From,
	request.To
)).WithOpenApi(op => {
	op.OperationId = "GetAvailableRooms";
	op.Description = "Finds available rooms in a hotel.";
	op.RequestBody.Description = "JSON describing the room search criteria.";
	op.RequestBody.Content[MediaTypeNames.Application.Json].Example = EncodeExample(getAvailableRoomsExample);
	return op;
})
.Accepts<GetAvailableRoomsRequest>(MediaTypeNames.Application.Json)
.Produces<IRoom[]>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

var createBookingExample = new CreateBookingRequest(
	1,
	102,
	today.AddDays(1),
	today.AddDays(10)
);
app.MapPost("/createBooking", (
	[FromBody] CreateBookingRequest request,
	[FromServices] HotelApi hotelApi
) => hotelApi.CreateBookingAsync(
	request.HotelId,
	request.RoomNumber,
	request.From,
	request.To
)).WithOpenApi(op => {
	op.OperationId = "CreateBooking";
	op.Description = "Creates a new booking.";
	op.RequestBody.Description = "JSON describing the booking information.";
	op.RequestBody.Content[MediaTypeNames.Application.Json].Example = EncodeExample(createBookingExample);
	return op;
})
.Accepts<CreateBookingRequest>(MediaTypeNames.Application.Json)
.Produces<ulong>((int)HttpStatusCode.OK, MediaTypeNames.Text.Plain);

app.MapGet("/getBooking/{id}", (
	[FromRoute] ulong id,
	[FromServices] HotelApi hotelApi
) => {
	var booking = hotelApi.GetBookingAsync(id);
	return booking;
}
).WithOpenApi(op => {
	op.OperationId = "GetBooking";
	op.Description = "Retrieves information for the booking with the given ID.";
	op.Parameters[0].Description = "The booking ID.";
	return op;
})
.Produces<IBooking>((int)HttpStatusCode.OK, MediaTypeNames.Application.Json);

app.Run();
