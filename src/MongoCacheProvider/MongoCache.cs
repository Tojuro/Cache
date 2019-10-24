namespace MongoCacheProvider
{
    using Cache.Interfaces;
    using MongoDB.Driver;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Runtime.Serialization.Formatters.Binary;

    public class MongoCache : ICacheProvider
    {
        private IMongoCollection<MongoCacheItem> mongoCollection;

        private DateTimeOffset DefaultAbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddDays(1));
        private TimeSpan DefaultAbsoluteExpirationRelativeToNow = new TimeSpan(1, 0, 0, 0);
        private TimeSpan DefaultSlidingExpiration = new TimeSpan(1, 0, 0, 0);

        public MongoCache()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("local");

            mongoCollection = database.GetCollection<MongoCacheItem>("inmem");
        }

        private IFindFluent<MongoCacheItem, MongoCacheItem> getQuery(string key)
        {
            return mongoCollection.Find<MongoCacheItem>(m => m.Key == key);
        }

        private static T Deserialize<T>(byte[] buffer)
        {
            var formatter = new BinaryFormatter();
            using (var ms = new MemoryStream(buffer))
            {
                using (var ds = new DeflateStream(ms, CompressionMode.Decompress, true))
                {
                    return (T)formatter.Deserialize(ds);
                }
            }
        }

        private static byte[] Serialize(Object obj)
        {
            var formatter = new BinaryFormatter();
            byte[] content;
            using (var ms = new MemoryStream())
            {
                using (var ds = new DeflateStream(ms, CompressionMode.Compress, true))
                {
                    formatter.Serialize(ds, obj);
                }
                ms.Position = 0;
                content = ms.GetBuffer();

                return content;
            }
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(string key)
        {
            return getQuery(key).Any();
        }

        public IEnumerable<string> Keys(Func<string, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            mongoCollection.DeleteMany(getQuery(key).Filter);
        }

        public T Retrieve<T>(string key)
        {
            return Deserialize<T>(getQuery(key).FirstOrDefault().Value);
        }

        public void Store(string key, object data, IDictionary<string, object> parameters)
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = parameters.TryGetValue("AbsoluteExpiration", out object s1)
                                                            && DateTimeOffset.TryParse(s1.ToString(), out DateTimeOffset t1)
                                                                ? t1 : DefaultAbsoluteExpiration,

                AbsoluteExpirationRelativeToNow = parameters.TryGetValue("AbsoluteExpirationRelativeToNow", out object s2)
                                                            && TimeSpan.TryParse(s2.ToString(), out TimeSpan t2)
                                                                ? t2 : DefaultAbsoluteExpirationRelativeToNow,

                SlidingExpiration = parameters.TryGetValue("SlidingExpiration", out object s3)
                                                            && TimeSpan.TryParse(s3.ToString(), out TimeSpan t3)
                                                                ? t3 : DefaultSlidingExpiration,
            };

            if (Contains(key))
            {
                mongoCollection.ReplaceOne(getQuery(key).Filter, new MongoCacheItem(key, Serialize(data), cacheEntryOptions));
            }
            else
            {
                mongoCollection.InsertOne(new MongoCacheItem(key, Serialize(data), cacheEntryOptions));
            }
        }
    }
}
