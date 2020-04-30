using System;
using System.Collections.Generic;

namespace Lykke.HftApi.Domain.Exceptions
{
    public class HftApiException : Exception
    {
        public HftApiErrorCode ErrorCode { get; set; }
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        public HftApiException(HftApiErrorCode code, string message):base(message)
        {
            ErrorCode = code;
        }

        public static HftApiException Create(HftApiErrorCode code, string message){
            return new HftApiException(code, message);
        }
    }

    public static class HftApiExceptionExtensions
    {
        public static HftApiException AddField(this HftApiException exception, string fieldName, string message)
        {
            exception.Fields.Add(fieldName, message);
            return exception;
        }

        public static HftApiException AddField(this HftApiException exception, string fieldName)
        {
            exception.Fields.Add(fieldName, exception.Message);
            return exception;
        }
    }
}
