// ViewModels/HomePageViewModel.cs
using System.Collections.Generic;
using ScentedCandleWebsite.Models;

namespace ScentedCandleWebsite.ViewModels
{
    public class HomePageViewModel
    {
        // Hero Section Data
        public string HeroTitle { get; set; } = "Welcome to Scented Candles";
        public string HeroSubtitle { get; set; } = "Handcrafted with love, scented with nature";

        // Categories for filtering
        public List<Category> Categories { get; set; } = new List<Category>();

        // Featured Products (shown at top)
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();

        // All Products (with filters applied)
        public List<Product> Products { get; set; } = new List<Product>();

        // Product Stats
        public int TotalProducts { get; set; }
        public int ProductsOnSale { get; set; }
        public int NewArrivals { get; set; }

        // Filter State
        public string SelectedCategory { get; set; }
        public string SearchTerm { get; set; }
        public string SortBy { get; set; } = "name";
    }
}