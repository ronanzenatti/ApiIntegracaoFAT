using ApiIntegracao.DTOs.Login;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiIntegracao.Infrastructure.Swagger
{
    /// <summary>
    /// Filtro para adicionar um exemplo padrão ao DTO de requisição de login no Swagger.
    /// </summary>
    public class LoginRequestSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            // Verifica se o schema atual é para o LoginRequestDto
            if (context.Type == typeof(LoginRequestDto))
            {
                // Define o objeto de exemplo com os valores desejados
                schema.Example = new OpenApiObject
                {
                    ["clientId"] = new OpenApiString("PortalFAT_App"),
                    ["clientSecret"] = new OpenApiString("UMA_SENHA_FORTE_E_SECRETA_PARA_O_PORTAL_GERADA_AQUI")
                };
            }
        }
    }
}