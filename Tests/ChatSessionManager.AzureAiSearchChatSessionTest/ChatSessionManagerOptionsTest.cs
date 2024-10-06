using ChatSessionManager.AzureAiSearchChatSession.Models.Options;
using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatSessionManager.AzureAiSearchChatSessionTest
{
    [TestClass]
    public class ChatSessionManagerOptionsTest 
    {
        [TestMethod]
        public void ChatSessionManagerOptionsNotNull()
        {
            IOptions<ChatSessionManagerOptions> options = AppHost.GetServiceProvider().GetRequiredService<IOptions<ChatSessionManagerOptions>>();
            Assert.IsNotNull(options);

        }

     
        
    }
}