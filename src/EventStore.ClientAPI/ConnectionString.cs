﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace EventStore.ClientAPI
{
    /// <summary>
    /// Methods for dealing with connection strings.
    /// </summary>
    public class ConnectionString
    {

        private static Dictionary<Type, Func<string, object>> translators;

        static ConnectionString()
        {
            translators = new Dictionary<Type, Func<string, object>>()
            {
                {typeof(int), x => int.Parse(x)},
                {typeof(decimal), x=>double.Parse(x)},
                {typeof(string), x => x},
                {typeof(bool), x=>bool.Parse(x)},
                {typeof(long), x=>long.Parse(x)},
                {typeof(byte), x=>byte.Parse(x)},
                {typeof(double), x=>double.Parse(x)},
                {typeof(float), x=>float.Parse(x)}
            };
        }

        /// <summary>
        /// Parses a connection string into its pieces represented as kv pairs
        /// </summary>
        /// <param name="connectionString">the connection string to parse</param>
        /// <returns></returns>
        private static IEnumerable<KeyValuePair<string, string>> GetConnectionStringInfo(string connectionString)
        {
            var builder = new DbConnectionStringBuilder(false) { ConnectionString = connectionString };
            //can someome mutate this builder before the enumerable is closed sure but thats the fun!
            return from object key in builder.Keys
                select new KeyValuePair<string, string>(key.ToString(), builder[key.ToString()].ToString());
        }

 
        /// <summary>
        /// Returns a <see cref="ConnectionSettings"></see> for a given connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>a <see cref="ConnectionSettings"/> from the connection string</returns>
        public static ConnectionSettings GetConnectionSettings(string connectionString)
        {
            var settings = ConnectionSettings.Default;
            var items = GetConnectionStringInfo(connectionString).ToArray();
            return Apply(items, settings);
        }

        private static T Apply<T>(IEnumerable<KeyValuePair<string , string>> items, T obj)
        {
            var fields = typeof (T).GetFields().Where(x=>x.IsPublic).ToDictionary(x => x.Name.ToLower(), x=>x);
            foreach (var item in items)
            {
                FieldInfo fi = null;
                if (!fields.TryGetValue(item.Key, out fi)) continue;
                Func<string, object> func = null;
                if (!translators.TryGetValue(fi.FieldType, out func))
                {
                    throw new Exception(string.Format("Can not map field named {0} as type {1} has no translator", item, fi.FieldType.Name));
                }
                fi.SetValue(obj, func(item.Value));
            }
            return obj;
        }
    }
}