// Exceptions/CettproExceptions.cs
namespace ApiIntegracao.Exceptions
{
    public class CettproApiException : Exception
    {
        public int StatusCode { get; }
        public string? ResponseContent { get; }

        public CettproApiException(int statusCode, string message, string? responseContent = null)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }

    public class CettproAuthenticationException : CettproApiException
    {
        public CettproAuthenticationException(string message)
            : base(401, message) { }
    }

    public class CettproResourceNotFoundException : CettproApiException
    {
        public string ResourceId { get; }

        public CettproResourceNotFoundException(string resourceId, string resourceType)
            : base(404, $"{resourceType} com ID {resourceId} não encontrado na CETTPRO")
        {
            ResourceId = resourceId;
        }
    }

    public class CettproRateLimitException : CettproApiException
    {
        public TimeSpan RetryAfter { get; }

        public CettproRateLimitException(TimeSpan retryAfter)
            : base(429, $"Rate limit excedido. Tente novamente em {retryAfter.TotalSeconds} segundos")
        {
            RetryAfter = retryAfter;
        }
    }
}