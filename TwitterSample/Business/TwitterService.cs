namespace TwitterSample
{
    public class TwitterService : HttpClient, ISampleService
    {
        public eServiceType ServiceType => eServiceType.Twitter;

        public TwitterService() { }

        public async Task<IEnumerable<ISample>> GetSampleAsync(bool reset = false, int limit = 0)
        {
            throw new NotImplementedException();
        }

        public async Task SampleStreamMiddleware(HttpContext context, RequestDelegate next)
        {
            throw new NotImplementedException();
        }

        public string GetSampleSocketURL(Uri baseAddress)
        {
            throw new NotImplementedException();
        }
    }
}
