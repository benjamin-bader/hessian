using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian
{
    public class DictionaryTypeResolver
    {
        private readonly Dictionary<string, Func<IDictionary<object, object>>> constructors;

        public DictionaryTypeResolver()
        {
            constructors.Add("System.Collections.Hashtable", DefaultCtor);
            constructors.Add("System.Collections.Generic.IDictionary`2", DefaultCtor);
            constructors.Add("System.Collections.Generic.Dictionary`2", DefaultCtor);
            constructors.Add("System.Collections.IDictionary", DefaultCtor);
            constructors.Add("java.lang.Map", DefaultCtor);
            constructors.Add("java.util.HashMap", DefaultCtor);
            constructors.Add("java.util.EnumMap", DefaultCtor);
            constructors.Add("java.util.TreeMap", DefaultCtor);
            constructors.Add("java.util.concurrent.ConcurrentHashMap", DefaultCtor);

        }

        public bool TryGetInstance(string type, out IDictionary<object, object> instance)
        {
            instance = null;

            Func<IDictionary<object, object>> ctor;
            if (!constructors.TryGetValue(type, out ctor)) {
                return false;
            }

            instance = ctor();
            return true;
        }

        private static IDictionary<object, object> DefaultCtor()
        {
            return new Dictionary<object, object>();
        }
    }
}
