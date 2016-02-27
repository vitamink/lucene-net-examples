using System.Reflection;
using Xunit;

namespace SpatialExamples
{
    public class UsageExample
    {
        [Fact]
        public void SearchAroundGermany1()
        {
            var searchProvider = CreateSearchProvider();

            var results = searchProvider.SearchByLocation("m* f*", 13.3833, 52.5167, 1000);
            Assert.Equal(3, results.Count);

            Assert.Equal("Frankfurt", results[0].Name);
            Assert.Equal("Munich", results[1].Name);
            Assert.Equal("Manchester", results[2].Name);
        }

        [Fact]
        public void SearchAroundGermany2()
        {
            var searchProvider = CreateSearchProvider();

            var results = searchProvider.SearchByLocation("f* p*", 13.3833, 52.5167, 1200);
            Assert.Equal(2, results.Count);

            Assert.Equal("Frankfurt", results[0].Name);
            Assert.Equal("Paris", results[1].Name);
        }

        private SearchProvider CreateSearchProvider()
        {
            var luceneDir = Assembly.GetExecutingAssembly().GetPathRelativeToAssembly("../../../lucene_index/");
            var searchProvider = new SearchProvider(luceneDir);
            searchProvider.DeleteAll();

            int id = 1;
            searchProvider.StoreItem(CreateSearchItem(id++, "London", 0.1275, 51.5072));
            searchProvider.StoreItem(CreateSearchItem(id++, "Paris", 2.3508, 48.8567));
            searchProvider.StoreItem(CreateSearchItem(id++, "Stockholm", 18.0686, 59.3294));
            searchProvider.StoreItem(CreateSearchItem(id++, "Munich", 11.5667, 48.1333));
            searchProvider.StoreItem(CreateSearchItem(id++, "Frankfurt", 8.6833, 50.1167));
            searchProvider.StoreItem(CreateSearchItem(id++, "Manchester", 2.2333, 53.4667));
            searchProvider.StoreItem(CreateSearchItem(id++, "Madrid", 3.7167, 40.4000));
            searchProvider.StoreItem(CreateSearchItem(id++, "Rome", 12.5000, 41.9000));
            searchProvider.StoreItem(CreateSearchItem(id++, "Tuscany", 11.0167, 43.3500));
            searchProvider.StoreItem(CreateSearchItem(id++, "Reykjavik", 21.9333, 64.1333));

            return searchProvider;
        }

        private SearchItem CreateSearchItem(int id, string name, double longitude, double latitude)
        {
            return new SearchItem()
            {
                Id = id,
                Name = name,
                Longitude = longitude,
                Latitude = latitude
            };
        }
    }
}
