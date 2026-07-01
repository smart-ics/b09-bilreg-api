using Bilreg.Api.Controllers.PaymentContext.TataRekeningFeature.Contracts;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bilreg.Api.Configurations;

public class TataRekeningExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(MergeBillingRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["mergeRequestId"] = new OpenApiString("MR-00001")
            };
            return;
        }

        if (context.Type == typeof(CancelFinalizationRequest) || context.Type == typeof(ReopenBillingRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["reason"] = new OpenApiString("Koreksi alokasi jaminan")
            };
        }
    }
}
