using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Shared.Events;
using ECommerce.Shared.Constants;
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
            // Validate user exists
            var userExists = await _userRepository.ExistsAsync(Guid.Parse(request.UserId));
            if (!userExists)
            {
                return CreateErrorResponse<OrderResponse>("User not found");
            }

            // Validate product exists and has enough stock
            var product = await _productRepository.GetByIdAsync(Guid.Parse(request.ProductId));
            if (product == null)
            {
                return CreateErrorResponse<OrderResponse>("Product not found");
            }

            if (!product.IsActive)
            {
                return CreateErrorResponse<OrderResponse>("Product is not available");
            }

            if (product.StockQuantity < request.Quantity)
            {
                return CreateErrorResponse<OrderResponse>("Insufficient stock");
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
            await _productRepository.UpdateStockAsync(request.ProductId, -request.Quantity);

            // Invalidate cache for this user
            var cacheKey = string.Format(AppConstants.Cache.UserOrdersKey, request.UserId);
            await _cacheService.RemoveAsync(cacheKey);

            // Publish order placed event
            var orderPlacedEvent = new OrderPlacedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                CorrelationId = GetCorrelationId()
            };

            await _messageBroker.PublishAsync(AppConstants.Queue.OrderPlacedQueue, orderPlacedEvent);

            var response = new OrderResponse
            {
                Id = order.Id,
                UserId = order.UserId,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt
            };

            _logger.LogInformation("Order created successfully: {OrderId}", order.Id);
            return CreateResponse(response, "Order created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user: {UserId}", request.UserId);
            return CreateErrorResponse<OrderResponse>("An error occurred while creating the order");
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<OrderListResponse>>> GetOrdersByUserId(string userId)
    {
        try
        {
            // Check cache first
            var cacheKey = string.Format(AppConstants.Cache.UserOrdersKey, userId);
            var cachedOrders = await _cacheService.GetAsync<OrderListResponse>(cacheKey);

            if (cachedOrders != null)
            {
                _logger.LogInformation("Retrieved orders from cache for user: {UserId}", userId);
                return CreateResponse(cachedOrders, "Orders retrieved from cache");
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

            _logger.LogInformation("Retrieved {Count} orders for user: {UserId}", orderResponses.Count, userId);
            return CreateResponse(response, "Orders retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for user: {UserId}", userId);
            return CreateErrorResponse<OrderListResponse>("An error occurred while retrieving orders");
        }
    }

    [HttpGet("{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> GetOrderById(Guid orderId)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return CreateErrorResponse<OrderResponse>("Order not found");
            }

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

            return CreateResponse(response, "Order retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order: {OrderId}", orderId);
            return CreateErrorResponse<OrderResponse>("An error occurred while retrieving the order");
        }
    }
}