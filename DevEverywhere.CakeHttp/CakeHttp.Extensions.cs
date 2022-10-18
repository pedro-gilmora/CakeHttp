using System.Reflection;
using CakeHttp.Attributes;
using CakeHttp.Enums;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace DevEverywhere.CakeHttp;

public static class CakeHttpExtensions
{

    public static IServiceCollection AddCakeHttp<TApi>(this IServiceCollection serviceCollection, string _url, bool camelCasePathAndQuery = false, EnumSerialization enumSerialization = EnumSerialization.CamelCaseString) where TApi : class 
    {
        AddCakeHttp<TApi>(serviceCollection, new CakeHttpOptionsAttribute(_url, camelCasePathAndQuery, enumSerialization));        
        return serviceCollection;
    }
    public static IServiceCollection AddCakeHttp<TApi>(this IServiceCollection serviceCollection, ICakeHttpInitOptions? _opts = null, Action<ICakeHttpInitOptions, HttpClient>? configureOptions = null) where TApi : class 
    {
        var apiType = typeof(TApi);
        if ((_opts ?? apiType.GetCustomAttribute<CakeHttpOptionsAttribute>()) is CakeHttpOptionsAttribute { BaseUrl: {} _url, CamelCasePathAndQuery: { } camelCasePathAndQuery } opts) 
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

                return CakeHttp.CreateClient<TApi>(httpClient, opts, apiType);
            }); 
        }
        
        return serviceCollection;
    }

}