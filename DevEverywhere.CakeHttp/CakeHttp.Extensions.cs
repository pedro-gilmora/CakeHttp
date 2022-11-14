using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Reflection;
using DevEverywhere.CakeHttp.Attributes;
using DevEverywhere.CakeHttp.Enums;
using DevEverywhere.CakeHttp.Inferfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DevEverywhere.CakeHttp;

public static class CakeHttpExtensions
{
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }

        return false;
    }

    public static IEnumerable<(TKey, TValue)> Deconstruct<TKey, TValue>(this IDictionary<TKey, TValue> keyValuePairs)
    {
        foreach (var item in keyValuePairs)
        {
            yield return (item.Key, item.Value);
        }
    }

    public static IServiceCollection AddCakeHttp<TApi>(this IServiceCollection serviceCollection, string _url, bool camelCasePathAndQuery = false, PropertyCasing enumSerialization = PropertyCasing.CamelCase) where TApi : class 
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