using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyFeatures.Helpers
{
    public class CustomOperationIdFilter : IOperationFilter
    {
        //ova metoda će se automatski pozvat, ne mora se eksplicitno
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];

            //mora bit unique
            operation.OperationId = $"{actionName}";
        }
    }
}
