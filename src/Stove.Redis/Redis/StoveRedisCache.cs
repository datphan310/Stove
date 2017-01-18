﻿using System;

using StackExchange.Redis;

using Stove.Domain.Entities;
using Stove.Runtime.Caching;

namespace Stove.Redis.Redis
{
    /// <summary>
    ///     Used to store cache in a Redis server.
    /// </summary>
    public class StoveRedisCache : CacheBase
    {
        private readonly IDatabase _database;
        private readonly IRedisCacheSerializer _serializer;

        /// <summary>
        ///     Constructor.
        /// </summary>
        public StoveRedisCache(
            string name,
            IStoveRedisCacheDatabaseProvider redisCacheDatabaseProvider,
            IRedisCacheSerializer redisCacheSerializer)
            : base(name)
        {
            _database = redisCacheDatabaseProvider.GetDatabase();
            _serializer = redisCacheSerializer;
        }

        public override object GetOrDefault(string key)
        {
            RedisValue objbyte = _database.StringGet(GetLocalizedKey(key));
            return objbyte.HasValue ? Deserialize(objbyte) : null;
        }

        public override void Set(string key, object value, TimeSpan? slidingExpireTime = null, TimeSpan? absoluteExpireTime = null)
        {
            if (value == null)
            {
                throw new StoveException("Can not insert null values to the cache!");
            }

            //TODO: This is a workaround for serialization problems of entities.
            Type type = value.GetType();
            if (EntityHelper.IsEntity(type) && type.Assembly.FullName.Contains("EntityFrameworkDynamicProxies"))
            {
                type = type.BaseType;
            }

            _database.StringSet(
                GetLocalizedKey(key),
                Serialize(value, type),
                absoluteExpireTime ?? slidingExpireTime ?? DefaultAbsoluteExpireTime ?? DefaultSlidingExpireTime
            );
        }

        public override void Remove(string key)
        {
            _database.KeyDelete(GetLocalizedKey(key));
        }

        public override void Clear()
        {
            _database.KeyDeleteWithPrefix(GetLocalizedKey("*"));
        }

        protected virtual string Serialize(object value, Type type)
        {
            return _serializer.Serialize(value, type);
        }

        protected virtual object Deserialize(RedisValue objbyte)
        {
            return _serializer.Deserialize(objbyte);
        }

        protected virtual string GetLocalizedKey(string key)
        {
            return "n:" + Name + ",c:" + key;
        }
    }
}
