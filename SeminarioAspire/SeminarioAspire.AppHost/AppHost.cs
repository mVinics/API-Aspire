
var builder = DistributedApplication.CreateBuilder(args);

// Redis para cache
var cache = builder.AddRedis("cache");

// PostgreSQL para persistência dos produtos
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("produtosdb");
    
// API de produtos
var apiService = builder.AddProject<Projects.SeminarioAspire_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health");

// Frontend do template, mantido temporariamente
builder.AddProject<Projects.SeminarioAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
