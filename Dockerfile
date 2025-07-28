FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
COPY ./ ./
WORKDIR /Peeveen.Hotel.Webservice
RUN dotnet publish -c Release -r linux-musl-x64 --self-contained

FROM alpine:latest
WORKDIR /app
RUN apk add gcompat libstdc++ icu-libs
COPY --from=build /Peeveen.Hotel.Webservice/bin/Release/net9.0/linux-musl-x64/publish/ ./
COPY --from=build /Peeveen.Hotel.Webservice/appsettings.yml ./
ENTRYPOINT ["/app/Peeveen.Hotel.Webservice"]