using RecipeAI.Interfaces;
using System.Diagnostics;

namespace RecipeAI.Services
{
    internal class ActivityProvider : IActivityProvider
    {
        public Activity Current => Activity.Current;
    }

}
