using RecipeAI.Interfaces;
using RecipeAI.Middlewares;

namespace RecipeAI.Extensions
{
    public static class ExceptionHandlerExtension
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = new ExceptionHandlerMiddleware(
                        app.ApplicationServices.GetRequiredService<ILoggerFactory>(),
                        app.ApplicationServices.GetRequiredService<IActivityProvider>()
                            ).Invoke,
                AllowStatusCode404Response = true
            });

            return app;
        }
    }
}
