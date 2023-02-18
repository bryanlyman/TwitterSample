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
    }
}