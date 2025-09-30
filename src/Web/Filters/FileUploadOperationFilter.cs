using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace stibe.api.Web.Filters
{
    /// <summary>
    /// Swagger operation filter to handle file upload operations with [FromForm] attributes
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasFileParameter = context.MethodInfo.GetParameters()
                .Any(param => param.ParameterType == typeof(IFormFile) || 
                             param.ParameterType == typeof(IFormFile[]));

            if (!hasFileParameter)
                return;

            // Clear existing parameters for file upload operations
            operation.Parameters?.Clear();

            // Set the request body for multipart/form-data
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = CreateFileUploadSchema(context)
                    }
                },
                Required = true
            };
        }

        private OpenApiSchema CreateFileUploadSchema(OperationFilterContext context)
        {
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>()
            };

            foreach (var parameter in context.MethodInfo.GetParameters())
            {
                var fromFormAttribute = parameter.GetCustomAttribute<FromFormAttribute>();
                if (fromFormAttribute == null) continue;

                if (parameter.ParameterType == typeof(IFormFile))
                {
                    schema.Properties[parameter.Name ?? "file"] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = "File to upload"
                    };
                }
                else if (parameter.ParameterType == typeof(IFormFile[]))
                {
                    schema.Properties[parameter.Name ?? "files"] = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        },
                        Description = "Files to upload"
                    };
                }
                else if (parameter.ParameterType == typeof(int))
                {
                    schema.Properties[parameter.Name ?? "id"] = new OpenApiSchema
                    {
                        Type = "integer",
                        Format = "int32",
                        Description = GetParameterDescription(parameter.Name)
                    };
                }
                else if (parameter.ParameterType == typeof(bool))
                {
                    schema.Properties[parameter.Name ?? "flag"] = new OpenApiSchema
                    {
                        Type = "boolean",
                        Description = GetParameterDescription(parameter.Name)
                    };
                }
                else if (parameter.ParameterType == typeof(string))
                {
                    schema.Properties[parameter.Name ?? "value"] = new OpenApiSchema
                    {
                        Type = "string",
                        Description = GetParameterDescription(parameter.Name)
                    };
                }
            }

            return schema;
        }

        private string GetParameterDescription(string? parameterName)
        {
            return parameterName switch
            {
                "staffId" => "ID of the staff member",
                "shopId" => "ID of the shop",
                "isProfileImage" => "Whether this is a profile image (true) or gallery image (false)",
                _ => $"The {parameterName} parameter"
            };
        }
    }
}