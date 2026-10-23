using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace FromToken;

/// <summary>
/// Provides a <see cref="UserIdModelBinder"/> for action parameters whose binding
/// source is <see cref="BindingSource.Custom"/>, i.e. parameters decorated with
/// <see cref="FromTokenAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// The provider is registered at the beginning of
/// <see cref="Microsoft.AspNetCore.Mvc.MvcOptions.ModelBinderProviders"/> by
/// <see cref="ServiceCollectionExtensions.AddFromTokenAttribute"/>, so it is
/// consulted before any built-in provider.
/// </para>
/// <para>
/// The binder is created via <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider)"/>,
/// which resolves the binder's constructor dependencies (such as
/// <see cref="Microsoft.Extensions.Options.IOptions{T}"/> for
/// <see cref="TokenUserOptions"/>) from the request services.
/// </para>
/// </remarks>
internal sealed class UserIdModelBinderProvider : IModelBinderProvider
{
    /// <summary>
    /// Returns a <see cref="UserIdModelBinder"/> when the parameter uses
    /// <see cref="BindingSource.Custom"/>; otherwise returns <see langword="null"/>.
    /// </summary>
    /// <param name="context">The model binder provider context.</param>
    /// <returns>
    /// A new <see cref="UserIdModelBinder"/> instance for custom-bound parameters,
    /// or <see langword="null"/> to let the next provider handle the parameter.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The check is based solely on <see cref="ModelBinderProviderContext.BindingInfo"/>'s
    /// <see cref="BindingInfo.BindingSource"/> being <see cref="BindingSource.Custom"/>.
    /// It does not verify that the parameter is decorated with
    /// <see cref="FromTokenAttribute"/> specifically — any parameter using
    /// <see cref="BindingSource.Custom"/> in the application will be handled by this
    /// binder.
    /// </remarks>
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.BindingInfo.BindingSource != BindingSource.Custom)
            return null;

        return ActivatorUtilities.CreateInstance<UserIdModelBinder>(context.Services);
    }
}