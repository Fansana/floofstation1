using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Kitchen
{
    public sealed class RecipeManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public List<FoodRecipePrototype> Recipes { get; private set; } = new();

        public void Initialize()
        {
            Recipes = new List<FoodRecipePrototype>();
            foreach (var item in _prototypeManager.EnumeratePrototypes<FoodRecipePrototype>())
            {
                if (!item.SecretRecipe)
                    Recipes.Add(item);
            }

            Recipes.Sort(new RecipeComparer());
        }
        /// <summary>
        /// Check if a prototype ids appears in any of the recipes that exist.
        /// </summary>
        public bool SolidAppears(string solidId)
        {
            return Recipes.Any(recipe => recipe.IngredientsSolids.ContainsKey(solidId));
        }

        private sealed class RecipeComparer : Comparer<FoodRecipePrototype>
        {
            public override int Compare(FoodRecipePrototype? x, FoodRecipePrototype? y)
            {
                if (x == null || y == null)
                {
                    return 0;
                }

                var nx = x.IngredientCount();
                var ny = y.IngredientCount();
                //Floofstation - Start
                //Added a fallback for recipes with the same results from IngredientCount
                //Original: return -nx.CompareTo(ny)
                if (-nx.CompareTo(ny) != 0)
                {
                    return -nx.CompareTo(ny);//If total solid ingredients and unique reagents are different, return result.
                }

                var vx = x.ReagentQuantity();
                var vy = y.ReagentQuantity();

                return -vx.CompareTo(vy);//Fallback result based on total amount of reagents.
                //Floofstation - End
            }
        }
    }
}
