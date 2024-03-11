using CryptoExchange;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SourceCrafter.HttpServiceClientGenerator.UnitTests;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SourceCrafter.HttpServiceClient.UnitTests
{
    public class QvaPayClientTests : TestingSetup, IClassFixture<QvaPayClient>
    {
        private readonly QvaPayClient _client;
        //static QvaPayClientTests()
        //{
        //    _config = new ConfigurationBuilder()
        //       .AddJsonFile("appsettings.json")
        //       .AddUserSecrets<TestingSetup>()
        //       .Build();
        //}
        public QvaPayClientTests(QvaPayClient clientSetup)
        {
            _client = clientSetup;
        }

        [Theory(DisplayName = "Should login")]
        [MemberData(nameof(GetData))]
        public async void ShouldLogin(bool useDedicatedService, Credentials credConfigs)
        {
            try
            {
                switch (await (useDedicatedService ? new LoginService() : _client.Auth.Login).PostAsync(credConfigs))
                {
                    case { StatusCode: HttpStatusCode.OK, OK.Me: { } me }:

                        me.Should().NotBeNull();
                        me.Name.Should().Be("Pedro Luis");

                        break;
                    case { StatusCode: HttpStatusCode.OK, UnprocessableEntity: { } errors }:

                        errors.Should().HaveCountGreaterThan(0);

                        break;
                }
            }
            catch (HttpRequestException e)
            {
                Trace.WriteLine(await e.TryRetrieveContentAsync<Dictionary<string, object>>());
                throw;
            }
        }

        public static List<object[]> GetData() =>
            new(){
                new object[] { true,    new Credentials("", "") },
                new object[] { false,   new Credentials("", "") },
                new object[] { true,    new Credentials("pedro@test.com", "1234$5643")},
                new object[] { false,   new Credentials("pedro@test.com", "1234$5643")},
                new object[] { true,    new Credentials("pedro@test.com", "1234$5643")},
                new object[] { false,    GetWorkingCredentials() }
            };

        private static Credentials GetWorkingCredentials()
             => new (_config["TestingData:Email"]!, _config["TestingData:Password"]!);

        [Theory(DisplayName = "Should retrieve transactions")]
        [InlineData(false)]
        [InlineData(true)]
        public async void ShouldRetrieveTransaction(bool useDedicatedService)
        {
            try
            {
                if (await _client.Auth.Login.PostAsync(GetWorkingCredentials()) is 
                    { 
                        StatusCode: HttpStatusCode.OK, 
                        OK: { } ok 
                    })
                {
                    _client.UpdateAuthenticationStatus(ok.AccessToken);

                    var list = await (useDedicatedService ? new TransactionsService() : _client.Transactions).GetAsync();

                    _client.UpdateAuthenticationStatus(null);

                    list.Should().NotBeNull();
                }
            }
            catch (HttpRequestException e)
            {
                Trace.WriteLine(await e.TryRetrieveContentAsync<Dictionary<string, object>>());
                throw;
            }
        }

    }
}