using RecipeAI.Extensions;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace RecipeAI.Serilog
{
    public class ActivityEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;
            if (activity is not null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty("TraceIdentifier", new ScalarValue(activity.GetCurrentTraceIdentifier())));
            }
        }
    }
}
