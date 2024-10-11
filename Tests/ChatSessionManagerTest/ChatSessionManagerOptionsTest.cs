using ChatSessionManager.Models.Options;
using ChatSessionManagerTest.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatSessionManagerTest
{
    [TestClass]
    public class ChatSessionManagerOptionsTest
    {
        private static ILogger<ChatSessionManagerOptionsTest> _logger;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _logger = AppHost.GetServiceProvider().GetRequiredService<ILogger<ChatSessionManagerOptionsTest>>();
            _logger.LogInformation(context.FullyQualifiedTestClassName);
        }

        [TestInitialize]
        public void Initialize()
        {
            _logger.LogInformation($"{nameof(ChatSessionManagerOptions)} test started");
        }
        [TestMethod]
        public void ChatSessionManagerOptionsNotNull()
        {

            IOptions<ChatSessionManagerOptions> options =
                AppHost.GetServiceProvider().GetRequiredService<IOptions<ChatSessionManagerOptions>>();
            _logger.LogInformation($"{nameof(ChatSessionManagerOptions)} is not null");
            Assert.IsNotNull(options);

        }



    }

}