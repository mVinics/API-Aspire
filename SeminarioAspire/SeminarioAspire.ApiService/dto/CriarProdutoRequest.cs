namespace SeminarioAspire.ApiService.Dtos;

public record CriarProdutoRequest(
    string Nome,
    decimal Preco,
    int Estoque
);