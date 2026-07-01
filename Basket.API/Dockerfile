FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends ca-certificates \
	&& update-ca-certificates \
	&& rm -rf /var/lib/apt/lists/*

COPY Basket.API/bin/Release/net10.0/publish/ .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Basket.API.dll"]
