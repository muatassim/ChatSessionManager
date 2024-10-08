using ChatSessionManager.AzureAiSearchChatSessionTest.Configuration;
using System.Diagnostics;

namespace ChatSessionManager.AzureAiSearchChatSessionTest
{
    [TestClass]
    public class GlobalTestSetup
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            DeleteFiles(AppHost.LogPath);
            if (!Directory.Exists(AppHost.LogPath))
                Directory.CreateDirectory(AppHost.LogPath);
            context.WriteLine("Assembly Init");
        }

        private static void DeleteFiles(string directoryPath)
        {
            //delete file
            if (Directory.Exists(directoryPath))
            {
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }

        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            OpenLogFolder();

        }

        private static void OpenLogFolder()
        {
            if (AppHost.OpenLogFolder)
            {
                try
                {
                    var psi = new ProcessStartInfo() { FileName = AppHost.LogPath, UseShellExecute = true };
                    Process.Start(psi);
                }
                catch { }
            }
        }
    }

}