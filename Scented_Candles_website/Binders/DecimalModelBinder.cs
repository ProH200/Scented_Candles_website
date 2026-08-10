using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace ScentedCandleWebsite.Models.Binders
{
    public class DecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            // Remove any currency symbols and spaces
            value = value.Replace("$", "").Replace("€", "").Replace("£", "").Trim();

            // Replace comma with period for decimal parsing
            value = value.Replace(",", ".");

            // Try to parse
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    "Invalid price format. Please enter a valid number (e.g., 24.99)");
            }

            return Task.CompletedTask;
        }
    }
}