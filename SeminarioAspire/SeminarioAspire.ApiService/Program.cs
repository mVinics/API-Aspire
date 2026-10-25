
using Microsoft.EntityFrameworkCore;
using SeminarioAspire.ApiService.Data;
using SeminarioAspire.ApiService.Dtos;
using SeminarioAspire.ApiService.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Registra o PostgreSQL utilizando a integração do Aspire.
// "produtosdb" deve corresponder ao nome definido no AppHost.
builder.AddNpgsqlDbContext<ProdutosDbContext>("produtosdb");

// Cache distribuído utilizando Redis
builder.AddRedisDistributedCache("cache");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Cria a estrutura inicial do banco para nossa demonstração.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProdutosDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// GET /produtos

app.MapGet("/produtos", async (
    ProdutosDbContext db,
    IDistributedCache cache,
    ILogger<Program> logger) =>
{
    const string cacheKey = "produtos_lista";

    // Primeiro verifica se os produtos estão no Redis.
    var produtosCache = await cache.GetStringAsync(cacheKey);

    if (produtosCache is not null)
    {
        logger.LogInformation("CACHE HIT: produtos recuperados do Redis.");

        var produtos = JsonSerializer.Deserialize<List<Produto>>(produtosCache);
        return Results.Ok(produtos);
    }

    // Se não estiverem no cache, consulta o PostgreSQL.
    logger.LogInformation("CACHE MISS: consultando produtos no PostgreSQL.");

    var produtosBanco = await db.Produtos
        .AsNoTracking()
        .OrderBy(p => p.Id)
        .ToListAsync();

    // Armazena o resultado no Redis por 5 minutos.
    await cache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(produtosBanco),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

    return Results.Ok(produtosBanco);
})
.WithName("ListarProdutos");


// GET /produtos/{id}
app.MapGet("/produtos/{id:int}", async (int id, ProdutosDbContext db) =>
{
    var produto = await db.Produtos
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id);

    return produto is null
        ? Results.NotFound("Produto não encontrado.")
        : Results.Ok(produto);
})
.WithName("BuscarProduto");

// POST /produtos

app.MapPost("/produtos", async (
    CriarProdutoRequest request,
    ProdutosDbContext db,
    IDistributedCache cache,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Nome) ||
        request.Nome.Trim().Length > 150 ||
        request.Preco <= 0 ||
        request.Estoque < 0)
    {
        return Results.BadRequest("Dados do produto inválidos.");
    }

    var produto = new Produto
    {
        Nome = request.Nome.Trim(),
        Preco = request.Preco,
        Estoque = request.Estoque
    };

    db.Produtos.Add(produto);
    await db.SaveChangesAsync();

    // Invalida a listagem armazenada no Redis.
    await cache.RemoveAsync("produtos_lista");

    logger.LogInformation(
        "Produto {ProdutoId} cadastrado. Cache da listagem invalidado.",
        produto.Id);

    return Results.Created($"/produtos/{produto.Id}", produto);
})
.WithName("CadastrarProduto");


app.MapDefaultEndpoints();

app.Run();
