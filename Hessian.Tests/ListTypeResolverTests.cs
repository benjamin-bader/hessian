using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Hessian.Tests
{
    [TestFixture]
    public class ListTypeResolverTests
    {
        [Test]
        public void TryGetInstance_WithListInterfaces_ProducesLists()
        {
            var resolver = new ListTypeResolver();
            var typeNames = new[] {typeof (IList).FullName, typeof (IList<object>).GetGenericTypeDefinition().FullName};
            foreach (var name in typeNames) {
                IList<object> list;
                Assert.IsTrue(resolver.TryGetListInstance(name, out list));
                Assert.NotNull(list);
            }
        }
    }
}
