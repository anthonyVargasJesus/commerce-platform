using Inventory.Application.Products.Dtos;
using MediatR;

namespace Inventory.Application.Products.Commands.AdjustStock;

public sealed record AdjustStockCommand(Guid ProductId, int Delta, string? IdempotencyKey = null) : IRequest<ProductDto>;
