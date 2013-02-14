using System;

namespace Hessian
{
    public class Conditions
    {
        protected Conditions()
        {
        }

        public static void CheckArgument(bool condition, string message, params object[] args)
        {
            if (condition) {
                return;
            }

            if (args.Length > 0) {
                message = String.Format(message, args);
            }

            throw new ArgumentException(message);
        }

        public static T CheckNotNull<T>(T value, string name)
            where T : class 
        {
            if (!ReferenceEquals(value, null)) {
                return value;
            }

            throw new ArgumentNullException(name);
        }

        public static TComparable CheckGreater<TComparable, TComparand>(TComparable value, TComparand bounds,
                                                                        string name)
            where TComparable : IComparable<TComparand>
        {
            if (value.CompareTo(bounds) > 0) {
                return value;
            }

            throw new ArgumentOutOfRangeException(name);
        }

        public static TComparable CheckLess<TComparable, TComparand>(TComparable value, TComparand bounds,
                                                                        string name)
            where TComparable : IComparable<TComparand>
        {
            if (value.CompareTo(bounds) < 0) {
                return value;
            }

            throw new ArgumentOutOfRangeException(name);
        }

        public static TComparable CheckGreaterOrEqual<TComparable, TComparand>(TComparable value, TComparand bounds,
                                                                        string name)
            where TComparable : IComparable<TComparand>
        {
            if (value.CompareTo(bounds) >= 0) {
                return value;
            }

            throw new ArgumentOutOfRangeException(name);
        }

        public static TComparable CheckLessOrEqual<TComparable, TComparand>(TComparable value, TComparand bounds,
                                                                        string name)
            where TComparable : IComparable<TComparand>
        {
            if (value.CompareTo(bounds) <= 0) {
                return value;
            }

            throw new ArgumentOutOfRangeException(name);
        }
    }
}
