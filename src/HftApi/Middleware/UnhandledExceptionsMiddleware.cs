using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using HftApi.Extensions;
using HftApi.WebApi.Models;
using Lykke.HftApi.Domain;
using Lykke.HftApi.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace HftApi.Middleware
{
    public class UnhandledExceptionsMiddleware
    {
        private readonly RequestDelegate _next;
        const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} {StatusCode} finished in {Elapsed:0.0000} ms";

        public UnhandledExceptionsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string body = null;
            var sw = Stopwatch.StartNew();
            try
            {
                body = await context.Request.GetBodyAsync();
                await _next.Invoke(context);
            }
            catch (HftApiException ex)
            {
                sw.Stop();
                await ErrorResponse(context, (int)HttpStatusCode.OK, ex.ErrorCode, ex.Message, ex.Fields);
                return;
            }
            catch (Exception ex)
            {
                sw.Stop();
                var logger = context.GetEnrichLogger(body);
                logger.Error(ex, ex.Message);
                await ErrorResponse(context,(int)HttpStatusCode.InternalServerError, HftApiErrorCode.RuntimeError, "Runtime error");
                return;
            }

            sw.Stop();

            if (context.Request.Path == "/api/isalive")
                return;

            context.GetEnrichLogger(body).Information(MessageTemplate,  context.Request.Method, $"{context.Request.Path}{context.Request.QueryString}", context.Response.StatusCode, sw.Elapsed.TotalMilliseconds);
        }

        private Task ErrorResponse(HttpContext ctx, int statusCode,
            HftApiErrorCode code, string message, Dictionary<string, string> fields = null)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = statusCode;

            var response = ResponseModel.Fail(code, message, fields ?? new Dictionary<string, string>());
            return ctx.Response.WriteAsync(JsonConvert.SerializeObject(response,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver()}));
        }
    }
}
