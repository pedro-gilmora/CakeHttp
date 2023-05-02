using FacilCuba.Infrastructure.QvaPay;
using FluentAssertions;
using SourceCrafter.HttpServiceClient.Constants;

namespace SourceCrafter.HttpServiceClient.UnitTests
{
    public class QvaPayClientTests : IClassFixture<QvaPayClient>
    {
        private readonly QvaPayClient _client;

        public QvaPayClientTests(QvaPayClient clientSetup)
        {
            _client = clientSetup;
            _client.DefaultQueryValues.Add("app_id", "faaa65b3-64b3-4c1c-90a7-25e3951662a5");
            _client.DefaultQueryValues.Add("app_secret", "a98zkqJPEnEmj6fFWgChIou8aAXW7Fwn03cpeaxlucnmJEgYAu");
        }

        [Fact]
        public async void ShouldLogin()
        {
            try
            {
                var e = await _client.Auth.Login.PostAsync(new()
                {
                    Email = "pedro.gilmora@outlook.es",
                    Password = "PlGM!21."
                });
                _client.UpdateAuthenticationStatus(e.AccessToken);
                e.Me.Should().NotBeNull();
            }
            catch (HttpRequestException e)
            {
                var res = await ((HttpContent)e.Data[ConstantValues.EXCEPTION_CONTENT]!).ReadAsStringAsync();
                throw;
            }
        }

        
    }

    public class QvaPayClientTestSetup : IDisposable
    {
        public QvaPayClient _client;

        public QvaPayClientTestSetup()
        {
        }

        public void Dispose()
        {
            _client = null!;
        }
    }
}