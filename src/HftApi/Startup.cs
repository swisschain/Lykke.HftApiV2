﻿using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Antares.Service.History.GrpcClient;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HftApi.Common.Configuration;
using HftApi.GrpcServices;
using HftApi.Middleware;
using HftApi.Modules;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Lykke.HftApi.Services.AssetsClient;
using Lykke.SettingsReader;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swisschain.Sdk.Server.Swagger;
using PrivateService = HftApi.GrpcServices.PrivateService;
using PublicService = HftApi.GrpcServices.PublicService;

namespace HftApi
{
    public sealed class Startup
    {
        public IConfiguration ConfigRoot { get; }
        public AppConfig Config;

        public Startup(IConfiguration configuration)
        {
            ConfigRoot = configuration;
            Config = ConfigRoot.Get<AppConfig>();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.Filters.Add(new ProducesAttribute("application/json"));
                })
                .AddNewtonsoftJson(options =>
                {
                    var namingStrategy = new CamelCaseNamingStrategy();

                    options.SerializerSettings.Converters.Add(new StringEnumConverter(namingStrategy));
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Include;
                    options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    options.SerializerSettings.Culture = CultureInfo.InvariantCulture;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = namingStrategy
                    };
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v2", new OpenApiInfo { Title = "Lykke Trading API", Description = $"Lykke trading API. See documentation <a target='_blank' href='{Config.DocumentationUrl}'>here</a>", Version = "v2" });
                c.EnableXmsEnumExtension();
                c.MakeResponseValueTypesRequired();
                c.AddJwtBearerAuthorization();
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            services.AddSwaggerGenNewtonsoftSupport();

            services.AddGrpc();
            services.AddGrpcReflection();

            services.AddCors(options => options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyHeader();
                builder.AllowAnyMethod();
                builder.AllowAnyOrigin();
            }));

            services.AddSingleton(Config);

            services
                .AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Config.Auth.JwtSecret)),
                        ValidateIssuer = false,
                        ValidateAudience = true,
                        ValidAudience = Config.Auth.LykkeAud,
                        ValidateLifetime = true
                    };
                });

            ConfigureServicesExt(services);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new AutofacModule(Config));
            builder.RegisterModule(new AutoMapperModule());
            builder.RegisterModule(new CqrsModule(Config.SagasRabbitMq));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStarted.Register(() =>
                app.ApplicationServices.GetService<StreamsManager>().Start()
            );

            applicationLifetime.ApplicationStopping.Register(() =>
                app.ApplicationServices.GetService<StreamsManager>().Stop()
            );

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<UnhandledExceptionsMiddleware>();

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseMiddleware<KeyCheckMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<MonitoringService>();
                endpoints.MapGrpcService<PrivateService>();
                endpoints.MapGrpcService<PublicService>();
                endpoints.MapGrpcReflectionService();
            });

            app.UseSwagger(c => c.RouteTemplate = "api/{documentName}/swagger.json");
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("../../api/v2/swagger.json", "API V2");
                c.RoutePrefix = "swagger/ui";
                c.EnableDeepLinking();
            });
        }

        private void ConfigureServicesExt(IServiceCollection services)
        {
            services.AddSingleton(Config.Auth);
            services.AddMemoryCache();
            services.AddHttpClient<AssetsHttpClient>(client =>
            {
                client.BaseAddress = new Uri(Config.Services.AssetsServiceUrl);
            });

            services.AddSingleton<IHistoryGrpcClient>(new HistoryGrpcClient(Config.Services.HistoryServiceUrl));
            services.AddTransient<HistoryWrapperClient>();

            services.AddHttpClient<BalanceHttpClient>(client =>
            {
                client.BaseAddress = new Uri(Config.Services.BalancesServiceUrl);
            });

            services.AddSingleton<ICacheService, CacheService>();
        }
    }
}
