FROM mcr.microsoft.com/dotnet/core/sdk:2.2

COPY Launcher /launcher

WORKDIR /launcher

RUN mkdir /app && \
mkdir /app/x64 && \
mkdir /app/arm64 && \
dotnet publish -o /app/x64 -c Release -f netcoreapp2.2 -r linux-x64 && \
dotnet publish -o /app/arm64 -c Release -f netcoreapp2.2 -r linux-arm64 && \
tar -zcvf launcher_binaries.tar.gz /app