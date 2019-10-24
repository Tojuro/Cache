#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cache
{
    using Cache.Interfaces;

    public class CacheProvider
    {
        private static ICacheProvider _provider = null;
        public CacheProvider(ICacheProvider provider)
        {
            _provider = provider;
        }

        public static ICacheProvider Provider {
            get {
                if (_provider == null)
                    throw new NoCacheProviderException();
                else
                    return _provider;
            }
        }
    }
}
