# Hotel API

Deployed to Azure at this address: [http://peeveenhotel.exagbhb3g8hgfsbn.ukwest.azurecontainer.io/swagger](http://peeveenhotel.exagbhb3g8hgfsbn.ukwest.azurecontainer.io/swagger)

NOTES

- I've used SQLite, and the use of the `OccupiedDates` table to prevent booking clashes is pretty inefficient, and a bit
  of a hack. I tried to stay within EFCore, but in a real-life implementation, I reckon it would be best to create a DB-specific function that checked for booking date clashes, and then use it in a `CONSTRAINT` on the bookings table.
- The spec mentions single, double and "deluxe" rooms, but doesn't mention what capacity "deluxe" is, so I just made
  the API support any capacity of room.
- Due to time constraints, some things I did not implement are:
  - public class/method comments.
  - webservice unit tests (the API tests cover most of the functionality)
  - logging (though I am familiar with the new `LoggerMessage` stuff from Microsoft)
  - metrics/tracing (using `ActivitySource`/`MeterFactory` etc)
  - `DbUpdateException` handling in HTTP response (contains a few unserializable properties)
