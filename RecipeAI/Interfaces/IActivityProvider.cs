using System.Diagnostics;

namespace RecipeAI.Interfaces
{
    public interface IActivityProvider
    {
        Activity Current { get; }
    }
}
