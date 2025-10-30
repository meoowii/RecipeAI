using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Formatting.Compact;


namespace RecipeAI.Serilog
{
    public static class SerilogHelper
    {
        public static Logger CreateLogger(IConfiguration config)
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .Enrich.With<ActivityEnricher>()
                .Enrich.WithMachineName()
                .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                    .WithDefaultDestructurers())
                .WriteTo.Console(new CompactJsonFormatter())
                .CreateLogger();
        }
    }
}
