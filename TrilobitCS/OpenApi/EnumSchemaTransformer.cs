using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace TrilobitCS.OpenApi;

// Enums are serialized as integers over the wire. By default OpenAPI emits them as a bare
// `{ "type": "integer" }`, so the frontend client generator only sees a plain number.
// This transformer adds the allowed integer values plus an `x-enum-varnames` extension
// (read by openapi-generator / NSwag / orval) so the generator can produce a named,
// typed enum (e.g. Gender.Male = 0) instead of an opaque int.
internal sealed class EnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        if (!type.IsEnum)
            return Task.CompletedTask;

        var names = Enum.GetNames(type);
        var values = Enum.GetValuesAsUnderlyingType(type);

        schema.Type = "integer";
        schema.Format = "int32";

        schema.Enum.Clear();
        foreach (var value in values)
            schema.Enum.Add(new OpenApiInteger(Convert.ToInt32(value)));

        var varNames = new OpenApiArray();
        foreach (var name in names)
            varNames.Add(new OpenApiString(name));
        schema.Extensions["x-enum-varnames"] = varNames;

        return Task.CompletedTask;
    }
}
