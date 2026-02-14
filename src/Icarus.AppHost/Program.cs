var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure containers
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("icarus-pgdata")
    .WithPgAdmin();

var postgresDb = postgres.AddDatabase("icarusdb");

var qdrant = builder.AddContainer("qdrant", "qdrant/qdrant", "latest")
    .WithHttpEndpoint(port: 6333, targetPort: 6333, name: "http")
    .WithEndpoint(port: 6334, targetPort: 6334, name: "grpc")
    .WithVolume("icarus-qdrantdata", "/qdrant/storage");

var couchdb = builder.AddContainer("couchdb", "couchdb", "3")
    .WithHttpEndpoint(port: 5984, targetPort: 5984, name: "http")
    .WithEnvironment("COUCHDB_USER", "admin")
    .WithEnvironment("COUCHDB_PASSWORD", "admin")
    .WithVolume("icarus-couchdata", "/opt/couchdb/data");

var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithVolume("icarus-miniodata", "/data");

var clickhouse = builder.AddContainer("clickhouse", "clickhouse/clickhouse-server", "latest")
    .WithHttpEndpoint(port: 8123, targetPort: 8123, name: "http")
    .WithVolume("icarus-clickhousedata", "/var/lib/clickhouse");

var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "http")
    .WithVolume("icarus-ollamadata", "/root/.ollama");

// Note: To add .NET project references, install the Aspire workload:
//   dotnet workload install aspire
// Then add <Sdk Name="Aspire.AppHost.Sdk" Version="9.1.0" /> to the csproj
// and use: builder.AddProject<Projects.Icarus_Orchestrator_Api>("orchestrator-api")

builder.Build().Run();
