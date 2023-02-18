namespace TwitterSample
{

    public enum eServiceType
    {
        Twitter,
        Reddit
    }

    public class ServiceFactory : IServiceProvider
    {
        public ServiceFactory() { }

        private static Type? GetServiceType(eServiceType serviceType)
        {
            switch (serviceType)
            {
                case eServiceType.Twitter: return typeof(TwitterService);
                case eServiceType.Reddit: return typeof(RedditService);
            }

            return null;
        }

        private static eServiceType? GetServiceEnum(Type serviceType)
        {
            if (serviceType == typeof(TwitterService)) return eServiceType.Twitter;
            if (serviceType == typeof(RedditService)) return eServiceType.Reddit;

            return null;
        }

        object? IServiceProvider.GetService(Type serviceType)
        {
            var eType = GetServiceEnum(serviceType);

            switch (eType)
            {
                case eServiceType.Twitter: return new TwitterService();
                case eServiceType.Reddit: return new RedditService();
            }


            return null;
        }

        public static ISampleService GetService(eServiceType serviceType)
        {
            var factory = new ServiceFactory() as IServiceProvider;
            var type = GetServiceType(serviceType);
            if (type != null) return factory.GetService(type) as ISampleService;
            return null;
        }
    }
}
