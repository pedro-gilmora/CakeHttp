using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SourceCrafter.HttpServiceClientGenerator.UnitTests
{
    public class TestingSetup
    {
        protected static IConfiguration _config = default!;

        static TestingSetup() {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<TestingSetup>()
                .Build();
        }
    }
}
