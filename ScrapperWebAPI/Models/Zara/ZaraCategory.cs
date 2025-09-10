namespace ScrapperWebAPI.Models.Zara
{
    public class ZaraCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SectionName { get; set; }
        public string Href { get; set; } // Href əlavə etdik
        public List<ZaraSubcategory> Subcategories { get; set; } = new List<ZaraSubcategory>();
    }

    public class ZaraSubcategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public string ParentSectionName { get; set; }
        public List<ZaraSubcategory> Children { get; set; } = new List<ZaraSubcategory>();
    }

    // Zara API-dan gələn JSON strukturu üçün helper class-lar
    public class ZaraApiResponse
    {
        public ZaraData Data { get; set; }
    }

    public class ZaraData
    {
        public List<ZaraSection> Sections { get; set; }
    }

    public class ZaraSection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public List<ZaraSubcategoryRaw> Subcategories { get; set; }
    }

    public class ZaraSubcategoryRaw
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Href { get; set; }
        public List<ZaraSubcategoryRaw> Subcategories { get; set; }
    }
}