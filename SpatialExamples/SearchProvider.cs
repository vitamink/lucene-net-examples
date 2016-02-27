using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Store;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace SpatialExamples
{
    public class SearchProvider
    {
        private readonly Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_30;

        private readonly string _luceneDir;
        private readonly SpatialContext _spatialContext;
        private readonly SpatialStrategy _strategy;

        private const string Id = "id";
        private const string Name = "name";
        private const string Location = "location";
        private const string Longitude = "longitude";
        private const string Latitude = "latitude";

        private FSDirectory _directory;
        private FSDirectory Directory
        {
            get
            {
                if (_directory == null)
                {
                    _directory = FSDirectory.Open(_luceneDir);

                    if (IndexWriter.IsLocked(_directory))
                    {
                        IndexWriter.Unlock(_directory);
                    }

                    var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                    if (File.Exists(lockFilePath))
                    {
                        File.Delete(lockFilePath);
                    }
                }

                return _directory;
            }
        }

        public SearchProvider(string luceneDir)
        {
            _luceneDir = luceneDir;
            _spatialContext = SpatialContext.GEO;
            int maxLevels = 11; // Results in sub-metre precision for geohash.
            SpatialPrefixTree grid = new GeohashPrefixTree(_spatialContext, maxLevels);
            _strategy = new RecursivePrefixTreeStrategy(grid, Location);
        }

        public void DeleteAll()
        {
            using (var analyser = new StandardAnalyzer(LuceneVersion))
            using (var writer = new IndexWriter(Directory, analyser, true, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                writer.DeleteAll();
            }
        }

        public void StoreItem(SearchItem item)
        {
            using (var analyser = new StandardAnalyzer(LuceneVersion))
            using (var writer = new IndexWriter(Directory, analyser, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                var searchQuery = new TermQuery(new Term($@"{Id}:""{item.Id}"""));
                writer.DeleteDocuments(searchQuery);
                writer.AddDocument(CreateDocument(item));
            }
        }

        private Document CreateDocument(SearchItem item)
        {
            var doc = new Document();

            doc.Add(new Field(Name, item.Name, Field.Store.YES, Field.Index.ANALYZED));

            // It's not strictly necessary to store the long/lat values explicitely, but could be useful with future Lucene versions.
            doc.Add(new NumericField(Longitude, 7, Field.Store.YES, true).SetDoubleValue(item.Longitude));
            doc.Add(new NumericField(Latitude, 7, Field.Store.YES, true).SetDoubleValue(item.Latitude));

            // These document values will be used when searching the index.
            var shape = (Shape)_spatialContext.MakePoint(item.Longitude, item.Latitude);
            foreach (var field in _strategy.CreateIndexableFields(shape))
            {
                doc.Add(field);
            }
            var point = (Point)shape;
            doc.Add(new Field(_strategy.GetFieldName(), $"{point.GetX().ToString("0.0000000")},{point.GetY().ToString("0.0000000")}", Field.Store.YES, Field.Index.NOT_ANALYZED));

            return doc;
        }

        public IList<SearchItem> SearchByLocation(string queryString, double longitude, double latitude, double searchRadiusKm, int maxHits = 10)
        {
            IList<SearchItem> results;

            using (var searcher = new IndexSearcher(Directory, true))
            using (var analyser = new StandardAnalyzer(LuceneVersion))
            {
                var distance = DistanceUtils.Dist2Degrees(searchRadiusKm, DistanceUtils.EARTH_MEAN_RADIUS_KM);
                var searchArea = _spatialContext.MakeCircle(longitude, latitude, distance);

                var fields = new[] {Name};
                var parser = new MultiFieldQueryParser(LuceneVersion, fields, analyser);
                parser.DefaultOperator = QueryParser.Operator.OR; // Allow multiple terms.
                var query = ParseQuery(queryString, parser);

                var spatialArgs = new SpatialArgs(SpatialOperation.Intersects, searchArea);
                var spatialQuery = _strategy.MakeQuery(spatialArgs);
                var valueSource = _strategy.MakeRecipDistanceValueSource(searchArea);
                var valueSourceFilter = new ValueSourceFilter(new QueryWrapperFilter(spatialQuery), valueSource, 0, 1);

                var filteredSpatial = new FilteredQuery(query, valueSourceFilter);
                var spatialRankingQuery = new FunctionQuery(valueSource);

                BooleanQuery bq = new BooleanQuery();
                bq.Add(filteredSpatial,Occur.MUST);
                bq.Add(spatialRankingQuery,Occur.MUST);

                var hits = searcher.Search(bq, maxHits).ScoreDocs;

                results = MapResultsToSearchItems(hits, searcher);
            }

            return results;
        }

        private static Query ParseQuery(string queryString, QueryParser parser)
        {
            Query query;

            try
            {
                query = parser.Parse(queryString.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(queryString.Trim()));
            }

            return query;
        }

        private IList<SearchItem> MapResultsToSearchItems(
            IList<ScoreDoc> hits,
            IndexSearcher searcher)
        {
            var orderedScoreDocs = hits.OrderByDescending(hit => hit.Score).ToList();
            return orderedScoreDocs.Select(hit => MapDocumentToSearchItem(searcher.Doc(hit.Doc))).ToList();
        }

        private SearchItem MapDocumentToSearchItem(Document doc)
        {
            var location = doc.Get(Location).Split(',');
            var searchItem = new SearchItem()
            {
                Name = doc.Get(Name),
                Longitude = double.Parse(location[0]),
                Latitude = double.Parse(location[1])
            };

            return searchItem;
        }
    }
}
