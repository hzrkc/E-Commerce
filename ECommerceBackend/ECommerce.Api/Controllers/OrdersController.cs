using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Interfaces;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Shared.Events;
using ECommerce.Shared.Constants;
using ECommerce.Core.Entities;
using ECommerce.Core.Enums;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : BaseController
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMessageBroker _messageBroker;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IMessageBroker messageBroker,
        ICacheService cacheService,
        ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _messageBroker = messageBroker;
        _cacheService = cacheService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Creating order for user: {UserId}, product: {ProductId}",
                request.UserId, request.ProductId);

            // Validate user exists
            if (!Guid.TryParse(request.UserId, out var userId))
            {
                return Error<OrderResponse>("Invalid user ID format");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Error<OrderResponse>("User not found", statusCode: 404);
            }

            // Validate product exists and availability
            if (!Guid.TryParse(request.ProductId, out var productId))
            {
                return Error<OrderResponse>("Invalid product ID format");
            }

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                return Error<OrderResponse>("Product not found", statusCode: 404);
            }

            if (!product.IsActive)
            {
                return Error<OrderResponse>("Product is not available");
            }

            if (product.StockQuantity < request.Quantity)
            {
                return Error<OrderResponse>("Insufficient stock");
            }

            // Create order
            var order = new Order
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                PaymentMethod = request.PaymentMethod,
                Status = OrderStatus.Pending,
                TotalAmount = product.Price * request.Quantity
            };

            await _orderRepository.AddAsync(order);

            // Update product stock
            product.StockQuantity -= request.Quantity;
            await _productRepository.UpdateAsync(product);

            // Invalidate cache
            var cacheKey = string.Format(AppConstants.Cache.UserOrdersKey, request.UserId);
            await _cacheService.RemoveAsync(cacheKey);

            // Publish event to RabbitMQ
            var orderEvent = new OrderPlacedEvent
            {
                OrderId = order.Id,
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                CorrelationId = GetCorrelationId()
            };

            await _messageBroker.PublishAsync(AppConstants.Queue.OrderPlacedQueue, orderEvent);

            var response = new OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                ProcessedAt = order.ProcessedAt
            };

            _logger.LogInformation("Order created successfully: {OrderId}", order.Id);
            return Success(response, "Order created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return Error<OrderResponse>("An error occurred while creating the order", statusCode: 500);
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<OrderListResponse>>> GetUserOrders(string userId)
    {
        try
        {
            _logger.LogInformation("Getting orders for user: {UserId}", userId);

            // Check cache first
            var cacheKey = string.Format(AppConstants.Cache.UserOrdersKey, userId);
            var cachedOrders = await _cacheService.GetAsync<OrderListResponse>(cacheKey);

            if (cachedOrders != null)
            {
                _logger.LogInformation("Orders retrieved from cache for user: {UserId}", userId);
                return Success(cachedOrders);
            }

            // Get from database
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
            var orderResponses = orders.Select(o => new OrderResponse
            {
                Id = o.Id,
                UserId = o.UserId,
                ProductId = o.ProductId,
                Quantity = o.Quantity,
                PaymentMethod = o.PaymentMethod,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                ProcessedAt = o.ProcessedAt
            }).ToList();

            var response = new OrderListResponse
            {
                Orders = orderResponses,
                TotalCount = orderResponses.Count
            };

            // Cache the result
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(AppConstants.Cache.DefaultTtlMinutes));

            _logger.LogInformation("Orders retrieved from database for user: {UserId}, count: {Count}",
                userId, orderResponses.Count);

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user: {UserId}", userId);
            return Error<OrderListResponse>("An error occurred while retrieving orders", statusCode: 500);
        }
    }
}