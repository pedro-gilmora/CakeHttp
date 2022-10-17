using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace CakeHttp;

public static class CakeHttpExtensions
{

    public static IServiceCollection AddCakeHttp<TApi>(this IServiceCollection serviceCollection, string? url = null, bool camelCasePathAndQuery = false) where TApi : class 
    {
        CakeHttpOptionsAttribute? restifyInitOptions = typeof(TApi).GetCustomAttribute<CakeHttpOptionsAttribute>();

        if ((url ?? restifyInitOptions?.BaseUrl) is { } _url) 
        {
            Uri baseUrl = new(_url);

            string authority = baseUrl.Authority;

            serviceCollection
                .AddHttpClient(authority, instance =>
                {
                    instance.BaseAddress = baseUrl;
                })
                .AddTransientHttpErrorPolicy(policy => 
                    policy.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1.5), retryCount: 5)));

            serviceCollection.AddScoped(services =>
            {
                var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(authority);
                return CakeHttp.CreateClient<TApi>(client, camelCasePathAndQuery);
            }); 
        }
        
        return serviceCollection;
    }
    public static IServiceCollection AddCakeHttp<TApi>(this IServiceCollection serviceCollection, Action<ICakeHttpInitOptions, HttpClient> configureOptions) where TApi : class 
    {
        if (typeof(TApi).GetCustomAttribute<CakeHttpOptionsAttribute>() is { BaseUrl: {} _url, CamelCasePathAndQuery: { } camelCasePathAndQuery } opts) 
        {
            Uri baseUrl = new(_url);

            string authority = baseUrl.Authority;

            serviceCollection
                .AddHttpClient(authority, instance =>
                {
                    instance.BaseAddress = baseUrl;
                })
                .AddTransientHttpErrorPolicy(policy => 
                    policy.WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1.5), retryCount: 5)));

            serviceCollection.AddScoped(services =>
            {
                var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient(authority);

                if (configureOptions is { })
                    configureOptions(opts, httpClient);

                return CakeHttp.CreateClient<TApi>(httpClient, opts);
            }); 
        }
        
        return serviceCollection;
    }

}