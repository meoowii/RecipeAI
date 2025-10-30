using System.Diagnostics;

namespace RecipeAI.Extensions
{
    public static class ActivityExtension
    {
        public static string GetCurrentTraceIdentifier(this Activity activity) => activity?.Id ?? string.Empty;
    }
}
