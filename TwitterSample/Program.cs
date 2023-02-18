namespace TwitterSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            BuildApp(builder);

            var app = builder.Build();

            AppConfigure(app);

            app.Run();
        }

        public static void BuildApp(WebApplicationBuilder builder)
        {
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Dependency Injection
            builder.Services.AddSingleton<ISampleService<RedditService>>(ServiceFactory.GetService(eServiceType.Reddit) as ISampleService<RedditService>);
        }

        public static void AppConfigure(WebApplication app)
        {
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseRedditSampleStream(); //Start sample stream service
        }

    }
}