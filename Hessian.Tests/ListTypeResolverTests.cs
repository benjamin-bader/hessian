using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ExpectBetter;

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
            var typeNames = new[]
                                {
                                    typeof (IList).FullName,
                                    typeof (IList<object>).GetGenericTypeDefinition().FullName
                                };
            foreach (var name in typeNames) {
                IList<object> list;
                var returnCode = resolver.TryGetListInstance(name, out list);
                Expect.The(returnCode).ToBeTrue();
                Expect.The(list).Not.ToBeNull();
            }
        }

        [Test]
        public void TryGetInstance_WithConcreteBclLists_ProducesLists()
        {
            var resolver = new ListTypeResolver();
            var types = new[]
                            {
                                typeof (ArrayList),
                                typeof (List<>),
                                typeof (Collection<>)
                            };

            foreach (var name in types.Select(t => t.FullName)) {
                IList<object> list;
                Assert.IsTrue(resolver.TryGetListInstance(name, out list));
                Assert.NotNull(list);
            }
        }
    }
}
