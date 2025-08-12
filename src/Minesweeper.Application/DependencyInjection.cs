using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Minesweeper.Application.Common;

namespace Minesweeper.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Add AutoMapper
        services.AddAutoMapper(config =>
        {
            config.AddMaps(assembly);
        });

        // Add FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Add validation behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}

/// <summary>
/// MediatR pipeline behavior for validation
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

            // Handle Result<T> responses
            if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Domain.Common.Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Domain.Common.Result)
                    .GetMethods()
                    .First(m => m.Name == nameof(Domain.Common.Result.Failure) && m.IsGenericMethodDefinition)
                    .MakeGenericMethod(resultType);
                return (TResponse)failureMethod.Invoke(null, new object[] { errorMessage })!;
            }

            // Handle Result responses
            if (typeof(TResponse) == typeof(Domain.Common.Result))
            {
                return (TResponse)(object)Domain.Common.Result.Failure(errorMessage);
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
