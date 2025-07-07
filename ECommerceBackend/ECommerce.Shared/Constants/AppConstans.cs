namespace ECommerce.Shared.Constants;

public static class AppConstants
{
    public static class Cache
    {
        public const string UserOrdersKey = "user_orders_{0}";
        public const int DefaultTtlMinutes = 2;
    }

    public static class Queue
    {
        public const string OrderPlacedQueue = "order-placed";
        public const string OrderProcessedQueue = "order-processed";
    }

    public static class Auth
    {
        public const string JwtSecretKey = "JWT_SECRET_KEY";
        public const string JwtIssuer = "ECommerceApi";
        public const string JwtAudience = "ECommerceClient";
        public const int JwtExpirationHours = 24;
    }

    public static class Logging
    {
        public const string CorrelationIdHeader = "X-Correlation-ID";
        public const string UserIdClaim = "user_id";
    }
}