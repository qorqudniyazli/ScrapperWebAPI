using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ScrapperWebAPI.Models.Zara;

namespace ScrapperWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ZaraController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ZaraController(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // Zara API üçün lazımi header-lar
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept",
                "application/json, text/javascript, */*; q=0.01");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "az,en;q=0.9");
            _httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        }

        [HttpGet("categories")]
        public async Task<ActionResult<List<ZaraCategory>>> GetCategories()
        {
            try
            {
                string url = "https://www.zara.com/az/ru/categories?ajax=true";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest($"API sorğusu uğursuz oldu. Status: {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();

                var categories = ParseZaraCategories(jsonContent);

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Xəta baş verdi: {ex.Message}");
            }
        }

        [HttpGet("subcategories")]
        public async Task<ActionResult<List<ZaraSubcategory>>> GetSubcategories([FromQuery] int? categoryId, [FromQuery] string categoryName)
        {
            try
            {
                string url = "https://www.zara.com/az/ru/categories?ajax=true";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest($"API sorğusu uğursuz oldu. Status: {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();

                var subcategories = GetSubcategoriesFromJson(jsonContent, categoryId, categoryName);

                return Ok(subcategories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Xəta baş verdi: {ex.Message}");
            }
        }

        [HttpGet("all-subcategories")]
        public async Task<ActionResult<List<ZaraSubcategory>>> GetAllSubcategories()
        {
            try
            {
                string url = "https://www.zara.com/az/ru/categories?ajax=true";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest($"API sorğusu uğursuz oldu. Status: {response.StatusCode}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();

                var allSubcategories = GetAllSubcategoriesFromJson(jsonContent);

                return Ok(allSubcategories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Xəta baş verdi: {ex.Message}");
            }
        }

        [HttpGet("raw-json")]
        public async Task<ActionResult<string>> GetRawJson()
        {
            try
            {
                string url = "https://www.zara.com/az/ru/categories?ajax=true";
                var response = await _httpClient.GetAsync(url);
                var jsonContent = await response.Content.ReadAsStringAsync();

                return Ok(new
                {
                    StatusCode = response.StatusCode,
                    Content = jsonContent,
                    Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value))
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Xəta: {ex.Message}");
            }
        }

        private List<ZaraCategory> ParseZaraCategories(string jsonContent)
        {
            var categories = new List<ZaraCategory>();

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                Console.WriteLine($"Root element type: {root.ValueKind}");

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        Console.WriteLine($"Property: {prop.Name} - Type: {prop.Value.ValueKind}");

                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            ExtractCategoriesFromArray(prop.Value, categories);
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            ExtractCategoriesFromObject(prop.Value, categories);
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array)
                {
                    ExtractCategoriesFromArray(root, categories);
                }

                Console.WriteLine($"Tapılan kateqoriya sayı: {categories.Count}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse xətası: {ex.Message}");
                throw;
            }

            return categories;
        }

        private void ExtractCategoriesFromArray(JsonElement arrayElement, List<ZaraCategory> categories)
        {
            foreach (var item in arrayElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var category = CreateCategoryFromElement(item);
                    if (category != null)
                    {
                        categories.Add(category);
                    }
                }
            }
        }

        private void ExtractCategoriesFromObject(JsonElement objectElement, List<ZaraCategory> categories)
        {
            // Object içindəki array property-ləri tapırıq
            foreach (var prop in objectElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    ExtractCategoriesFromArray(prop.Value, categories);
                }
            }
        }

        private ZaraCategory CreateCategoryFromElement(JsonElement element)
        {
            var id = GetIntProperty(element, "id");
            var name = GetStringProperty(element, "name") ??
                       GetStringProperty(element, "title") ??
                       GetStringProperty(element, "label");

            var sectionName = GetStringProperty(element, "sectionName") ??
                             GetStringProperty(element, "section") ??
                             GetStringProperty(element, "category") ??
                             GetStringProperty(element, "type");

            var href = GetStringProperty(element, "href") ??
                       GetStringProperty(element, "url") ??
                       GetStringProperty(element, "link");

            if (!string.IsNullOrEmpty(name))
            {
                var category = new ZaraCategory
                {
                    Id = id,
                    Name = name,
                    SectionName = sectionName ?? "Unknown",
                    Href = href
                };

                // Subcategory-ləri tap
                if (element.TryGetProperty("subcategories", out var subcategoriesElement) ||
                    element.TryGetProperty("children", out subcategoriesElement) ||
                    element.TryGetProperty("items", out subcategoriesElement))
                {
                    category.Subcategories = ExtractSubcategoriesFromElement(subcategoriesElement, sectionName ?? name);
                }

                return category;
            }

            return null;
        }

        private List<ZaraSubcategory> ExtractSubcategoriesFromElement(JsonElement subcategoriesElement, string parentSectionName)
        {
            var subcategories = new List<ZaraSubcategory>();

            if (subcategoriesElement.ValueKind != JsonValueKind.Array) return subcategories;

            foreach (var subcategory in subcategoriesElement.EnumerateArray())
            {
                var id = GetIntProperty(subcategory, "id");
                var name = GetStringProperty(subcategory, "name");
                var href = GetStringProperty(subcategory, "href") ??
                           GetStringProperty(subcategory, "url") ??
                           GetStringProperty(subcategory, "link");

                if (!string.IsNullOrEmpty(name))
                {
                    var zaraSubcat = new ZaraSubcategory
                    {
                        Id = id,
                        Name = name,
                        Href = href,
                        ParentSectionName = parentSectionName
                    };

                    // Nested subcategories
                    if (subcategory.TryGetProperty("subcategories", out var nestedSubcategories))
                    {
                        zaraSubcat.Children = ExtractSubcategoriesFromElement(nestedSubcategories, parentSectionName);
                    }

                    subcategories.Add(zaraSubcat);
                }
            }

            return subcategories;
        }

        private List<ZaraSubcategory> GetSubcategoriesFromJson(string jsonContent, int? categoryId, string categoryName)
        {
            var allSubcategories = new List<ZaraSubcategory>();

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                SearchSubcategoriesInElement(root, categoryId, categoryName, allSubcategories);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse xətası: {ex.Message}");
                throw;
            }

            return allSubcategories;
        }

        private List<ZaraSubcategory> GetAllSubcategoriesFromJson(string jsonContent)
        {
            var allSubcategories = new List<ZaraSubcategory>();

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                ExtractAllSubcategoriesFromElement(root, allSubcategories);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parse xətası: {ex.Message}");
                throw;
            }

            return allSubcategories;
        }

        private void SearchSubcategoriesInElement(JsonElement element, int? categoryId, string categoryName, List<ZaraSubcategory> result)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var id = GetIntProperty(element, "id");
                var name = GetStringProperty(element, "name");

                bool isTargetCategory = false;
                if (categoryId.HasValue && id == categoryId.Value)
                    isTargetCategory = true;
                else if (!string.IsNullOrEmpty(categoryName) && !string.IsNullOrEmpty(name) &&
                         name.Contains(categoryName, StringComparison.OrdinalIgnoreCase))
                    isTargetCategory = true;

                if (isTargetCategory && element.TryGetProperty("subcategories", out var subcategoriesElement))
                {
                    var subcategories = ExtractSubcategoriesFromElement(subcategoriesElement, name);
                    result.AddRange(subcategories);
                }

                // Digər property-lərə də bax
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array || prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        SearchSubcategoriesInElement(prop.Value, categoryId, categoryName, result);
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    SearchSubcategoriesInElement(item, categoryId, categoryName, result);
                }
            }
        }

        private void ExtractAllSubcategoriesFromElement(JsonElement element, List<ZaraSubcategory> result)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var name = GetStringProperty(element, "name");

                if (element.TryGetProperty("subcategories", out var subcategoriesElement))
                {
                    var subcategories = ExtractSubcategoriesFromElement(subcategoriesElement, name);
                    result.AddRange(subcategories);
                }

                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array || prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        ExtractAllSubcategoriesFromElement(prop.Value, result);
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    ExtractAllSubcategoriesFromElement(item, result);
                }
            }
        }

        private string GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
            return null;
        }

        private int GetIntProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number)
                {
                    return property.GetInt32();
                }
                else if (property.ValueKind == JsonValueKind.String &&
                         int.TryParse(property.GetString(), out var intValue))
                {
                    return intValue;
                }
            }
            return 0;
        }
    }
}