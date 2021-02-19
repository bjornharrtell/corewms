using System;

namespace CoreWms
{
    public class ServiceException : Exception
    {
        public ServiceException(string? message) : base(message) {}
    }

    public class LayerNotDefinedException : ServiceException
    {
        public LayerNotDefinedException(string? message) : base(message) {}
    }
}