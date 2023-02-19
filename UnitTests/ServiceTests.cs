namespace UnitTests
{
    [TestClass]
    public class ServiceTests
    {
        [TestMethod]
        public void ServiceFactory_ServiceType_Twitter()
        {
            Assert.AreEqual(ServiceFactory.GetService(eServiceType.Twitter).GetType(), typeof(TwitterService));
        }

        [TestMethod]
        public void ServiceFactory_ServiceType_Reddit()
        {
            Assert.AreEqual(ServiceFactory.GetService(eServiceType.Reddit).GetType(), typeof(RedditService));
        }

        [TestMethod]
        public void RedditService_GetSampleSocketURL_HTTPS()
        {
            var uri = new Uri("https://127.0.0.1:3030");
            var redditService = ServiceFactory.GetService(eServiceType.Reddit);
            var socketUrl = redditService.GetSampleSocketURL(uri);
            Assert.AreEqual(socketUrl, "wss://127.0.0.1:3030/redditstream");
        }

        [TestMethod]
        public void RedditService_GetSampleSocketURL_HTTP()
        {
            var uri = new Uri("http://127.0.0.1:3030");
            var redditService = ServiceFactory.GetService(eServiceType.Reddit);
            var socketUrl = redditService.GetSampleSocketURL(uri);
            Assert.AreEqual(socketUrl, "ws://127.0.0.1:3030/redditstream");
        }

    }
}