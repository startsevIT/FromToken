using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FromToken;

/// <summary>
/// A Swashbuckle operation filter that removes parameters decorated with
/// <see cref="FromTokenAttribute"/> from the generated OpenAPI operation.
/// </summary>
/// <remarks>
/// <para>
/// Parameters bound via <see cref="FromTokenAttribute"/> use
/// <see cref="Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Custom"/>,
/// which Swashbuckle has no built-in schema/source mapping for. By default such
/// parameters may be emitted into the OpenAPI document as regular parameters
/// (often as query parameters), which is misleading because they are not supplied
/// by the client — they are resolved server-side from a token.
/// </para>
/// <para>
/// This filter inspects the action method's parameters, collects the names of those
/// marked with <see cref="FromTokenAttribute"/>, and removes the corresponding
/// entries from <see cref="OpenApiOperation.Parameters"/>. The result is an OpenAPI
/// document that accurately reflects the client contract: token-bound parameters are
/// not part of the request.
/// </para>
/// <para>
/// If the method has no parameters decorated with <see cref="FromTokenAttribute"/>,
/// the filter exits immediately without modifying the operation.
/// </para>
/// </remarks>
/// <example>
/// Register the filter when configuring Swagger:
/// <code>
/// builder.Services.AddSwaggerGen(options =>
/// {
///     options.OperationFilter&lt;FromTokenOperationFilter&gt;();
/// });
/// </code>
/// </example>
internal sealed class FromTokenOperationFilter : IOperationFilter
{
    /// <summary>
    /// Removes all parameters decorated with <see cref="FromTokenAttribute"/>
    /// from the given OpenAPI operation.
    /// </summary>
    /// <param name="operation">
    /// The OpenAPI operation being generated. Its <see cref="OpenApiOperation.Parameters"/>
    /// collection is modified in place.
    /// </param>
    /// <param name="context">
    /// The filter context providing access to the action's <see cref="OperationFilterContext.MethodInfo"/>.
    /// </param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parametersWithFromToken = context.MethodInfo
            .GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(FromTokenAttribute), inherit: true).Length != 0)
            .Select(p => p.Name)
            .ToHashSet();

        if (parametersWithFromToken.Count == 0)
            return;

        operation.Parameters = operation.Parameters
            .Where(p => !parametersWithFromToken.Contains(p.Name))
            .ToList();
    }
}