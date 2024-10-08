using ChatSessionManager.AzureAiSearchChatSession.Models.Options;
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatSessionManager.AzureAiSearchChatSessionTest
{
    [TestClass]
    public class ChatSessionManagerOptionsTest
    {
        private readonly ILogger<ChatSessionManagerOptionsTest> _logger;
        public ChatSessionManagerOptionsTest()
        {
            _logger = AppHost.GetServiceProvider().GetRequiredService<ILogger<ChatSessionManagerOptionsTest>>();

        }
        [TestInitialize]
        public void Initialize()
        {
            _logger.LogInformation($"{nameof(ChatSessionManagerOptions)} test started");
        }
        [TestMethod]
        public void ChatSessionManagerOptionsNotNull()
        {

            IOptions<ChatSessionManagerOptions> options = AppHost.GetServiceProvider().GetRequiredService<IOptions<ChatSessionManagerOptions>>();
            Assert.IsNotNull(options);

        }



    }

}