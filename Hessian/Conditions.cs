using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hessian
{
    public static class Conditions
    {
        public static T CheckNotNull<T>(T value, string name)
            where T : class 
        {
            if (!ReferenceEquals(value, null)) {
                return value;
            }

            throw new ArgumentNullException(name);
        }
    }
}
