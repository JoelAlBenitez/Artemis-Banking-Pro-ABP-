using Microsoft.OpenApi.Models;

namespace ArtemisBankingPro.Presentation.WebApi.Extensions
{
    public static class SwaggerExtension
    {
        private const string SecuritySchemeName = "Bearer";

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.0",
                    Title = "Artemis Banking Pro - Web API",
                    Description = "Endpoints administrativos del banco y procesador de pagos Hermes Pay."
                });

                //Los <summary> y <example> de Commands, Queries y controladores son la
                //descripción del body y de los parámetros en Swagger.
                foreach (var xmlFile in Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    options.IncludeXmlComments(xmlFile);
                }

                options.DescribeAllParametersInCamelCase();
                options.EnableAnnotations();

                options.AddSecurityDefinition(SecuritySchemeName, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = SecuritySchemeName,
                    BearerFormat = "JWT",
                    Description = "Ingrese el token con el formato 'Bearer {su token}'."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = SecuritySchemeName
                            },
                            Scheme = SecuritySchemeName,
                            Name = SecuritySchemeName,
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });

            return services;
        }

        public static WebApplication UseSwaggerDocumentation(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Artemis Banking Pro - Web API v1.0");
                options.RoutePrefix = "swagger";
            });

            return app;
        }
    }
}
