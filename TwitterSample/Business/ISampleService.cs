namespace TwitterSample
{
    public interface ISampleService
    {
        eServiceType ServiceType { get; }


        /// <summary>
        /// Get collection of feed samples
        /// </summary>
        /// <param name="reset">When sampling from a specific index: If true, reset to start</param>
        /// <param name="limit">The limit of samples. 0 means service default</param>
        Task<IEnumerable<ISample>> GetSampleAsync(bool reset = false, int limit = 0);

        Task SampleStreamMiddleware(HttpContext context, RequestDelegate next);

        string GetSampleSocketURL(Uri baseAddress);
    }

    public interface ISampleService<out HttpClient> : ISampleService
    {

    }

}
