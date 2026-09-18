using MediatR;

namespace Notifications.Application.Notifications.Commands.SendStockAlert;

public sealed record SendStockAlertCommand(Guid ProductId, string Sku, string Name, int QuantityOnHand, int ReorderLevel) : IRequest;
