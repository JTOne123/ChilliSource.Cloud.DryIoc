using ChilliSource.Cloud.Core;
using DryIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ChilliSource.Cloud.DryIoc.Tests
{
    public class ScopeContextTests : IDisposable
    {
        IScopeContextFactory scopeContextFactory;

        public ScopeContextTests()
        {
            scopeContextFactory = ScopeContextHelper.CreateFactory(FactorySetup);
        }

        private void FactorySetup(IScopeContextFactorySetup binder)
        {
            binder.RegisterServices(RegisterServices);

            binder.RegisterSingletonType(typeof(CustomValue));
        }

        private static PropertyOrFieldServiceInfo WithInfo(MemberInfo member, Request request)
        {
            var isInjectable = member.GetCustomAttributes(typeof(ServiceInjectionAttribute), false).Length > 0;

            return isInjectable ? PropertyOrFieldServiceInfo.Of(member) : null;
        }

        private void RegisterServices(IContainer container, IReuse reuse)
        {
            var propertyResolution = PropertiesAndFields.All(withNonPublic: true, withPrimitive: false, withFields: false, ifUnresolved: IfUnresolved.ReturnDefault, withInfo: WithInfo);

            container.Register<MyServiceA>(reuse, Made.Of(propertiesAndFields: propertyResolution));
            container.Register<MyServiceB>(reuse, Made.Of(propertiesAndFields: propertyResolution));

            var properties = PropertiesAndFields.All(withInfo: ServiceCHelperInfo.GetInfo);
            container.Register<MyServiceC>(reuse, made: Made.Of(propertiesAndFields: properties));
        }

        [Fact]
        public void TestSimpleService()
        {
            MyServiceB.DoStuffCount = 0;
            using (var scope = scopeContextFactory.CreateScope())
            {
                var svc = scope.Get<MyServiceB>();

                svc.DoStuff();

                Assert.True(MyServiceB.DoStuffCount == 1);
            }
        }

        [Fact]
        public void TestGCCollect()
        {
            Core.IScopeContext scope = null;

            for (int i = 0; i < 10; i++)
            {
                scope = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();

                MyServiceB.DoStuffCount = 0;
                using (scope = scopeContextFactory.CreateScope())
                {
                    var svc = scope.Get<MyServiceB>();

                    svc.DoStuff();

                    Assert.True(MyServiceB.DoStuffCount == 1);
                    svc = null;
                }
            }
        }

        [Fact]
        public void TestDifferentScopes()
        {
            Core.IScopeContext scope1 = null, scope2 = null;
            MyServiceA svc1, svc2;
            CustomValue value1, value2;

            try
            {
                scope1 = scopeContextFactory.CreateScope();
                scope1.SetSingletonValue(new CustomValue() { Value = 100 });
                svc1 = scope1.Get<MyServiceA>();
                value1 = scope1.GetSingletonValue<CustomValue>();

                scope2 = scopeContextFactory.CreateScope();
                svc2 = scope2.Get<MyServiceA>();
                value2 = scope2.GetSingletonValue<CustomValue>();

                Assert.False(object.ReferenceEquals(scope1, scope2));
                Assert.False(object.ReferenceEquals(svc1, svc2));
                Assert.False(object.ReferenceEquals(value1, value2));
            }
            finally
            {
                if (scope1 != null) scope1.Dispose();
                if (scope2 != null) scope2.Dispose();
            }
        }

        [Fact]
        public void TestScopeContextInstances()
        {
            scopeContextFactory.Execute<MyServiceA>(svc =>
            {
                svc.DoStuff();

                var anotherInstance = svc.Resolver.Get<MyServiceA>();

                Assert.True(Object.ReferenceEquals(svc, anotherInstance));
            });
        }

        [Fact]
        public void TestScopeContextDependency()
        {
            MyServiceB.DoStuffCount = 0;

            scopeContextFactory.Execute<MyServiceA>(svc =>
            {
                svc.DoStuff();
            });

            Assert.True(MyServiceB.DoStuffCount > 0);
        }

        [Fact]
        public void TestScopeContextAutoDispose()
        {
            MyServiceA.DisposedCount = 0;
            MyServiceB.DisposedCount = 0;

            scopeContextFactory.Execute<MyServiceA>(svc =>
            {
                svc.DoStuff();
            });

            Assert.True(MyServiceA.DisposedCount > 0);
            Assert.True(MyServiceB.DisposedCount > 0);
        }

        [Fact]
        public void TestScopeContextSingletonValue()
        {
            scopeContextFactory.Execute<MyServiceA>(svc =>
            {
                Assert.True(svc.CustomValue == null);
            });

            scopeContextFactory.Execute<MyServiceA>(scope => scope.SetSingletonValue<CustomValue>(new CustomValue() { Value = 8 }), svc =>
            {
                Assert.True(svc.CustomValue != null && svc.CustomValue.Value == 8);
            });

            scopeContextFactory.Execute<MyServiceA>(scope => scope.SetSingletonValue<CustomValue>(new CustomValue() { Value = 123 }), svc =>
            {
                Assert.True(svc.CustomValue != null && svc.CustomValue.Value == 123);
            });

            scopeContextFactory.Execute<MyServiceA>(scope => scope.SetSingletonValue<CustomValue>(new CustomValue() { Value = 123 }), svc =>
            {
                Assert.True(svc.CustomValue != null && svc.CustomValue.Value == 123);
            });
        }

        [Fact]
        public void TestScopeContextTask()
        {
            var signal = new ManualResetEvent(false);

            scopeContextFactory.ExecuteAsync<MyServiceA>(scope => scope.SetSingletonValue<CustomValue>(new CustomValue() { Value = 321 }), svc =>
            {
                Assert.True(svc.CustomValue != null && svc.CustomValue.Value == 321);
                signal.Set();
            });

            signal.WaitOne();
        }

        [Fact]
        public void TestPropertySelector()
        {
            ServiceCHelperInfo.GetInfoCalls = 0;

            using (var scope = scopeContextFactory.CreateScope())
            {
                var svc = scope.Get<MyServiceC>();
                Assert.True(svc != null);
                Assert.True(svc.ServiceA != null);

                Assert.True(ServiceCHelperInfo.GetInfoCalls == 1);
                var svc2 = scope.Get<MyServiceC>();

                Assert.True(Object.ReferenceEquals(svc, svc2));
                Assert.True(ServiceCHelperInfo.GetInfoCalls == 1);
            }

            using (var anotherScope = scopeContextFactory.CreateScope())
            {
                var anotherSvc = anotherScope.Get<MyServiceC>();
                Assert.True(anotherSvc != null);

                Assert.True(ServiceCHelperInfo.GetInfoCalls == 1);
            }
        }

        static int _limitValue;
        [Fact]
        public void TestIncrementLimit()
        {
            _limitValue = int.MaxValue;
            var incremented = (uint) Interlocked.Increment(ref _limitValue);

            Assert.True(incremented == (uint)int.MaxValue + (uint)1);
        }

        public void Dispose()
        {
            scopeContextFactory.Dispose();
        }

        public class MyServiceA : IDisposable
        {
            public static int DisposedCount;

            MyServiceB _service2;
            [ServiceInjection]
            public CustomValue CustomValue { get; private set; }

            [ServiceInjection]
            public Core.IServiceResolver Resolver { get; private set; }

            public MyServiceA(MyServiceB service2)
            {
                _service2 = service2;
            }

            public void DoStuff()
            {
                if (_disposed)
                    throw new ObjectDisposedException("MyServiceA");

                _service2.DoStuff();
            }

            private bool _disposed;
            public void Dispose()
            {
                DisposedCount++;

                if (!_disposed)
                {
                    _disposed = true;
                }
            }
        }

        public class CustomValue
        {
            public int Value { get; set; }
        }

        public class MyServiceB : IDisposable
        {
            public static int DisposedCount;
            public static int DoStuffCount;

            public void Dispose()
            {
                DisposedCount++;
            }

            internal void DoStuff()
            {
                DoStuffCount++;
            }
        }

        public class MyServiceC
        {
            public MyServiceA ServiceA { get; set; }

            internal void DoStuff()
            {
            }
        }

        public static class ServiceCHelperInfo
        {
            static int _getInfoCalls;
            public static int GetInfoCalls { get { return _getInfoCalls; } set { _getInfoCalls = value; } }

            internal static PropertyOrFieldServiceInfo GetInfo(MemberInfo member, Request request)
            {
                if ((member as PropertyInfo)?.PropertyType == typeof(MyServiceA))
                {
                    Interlocked.Increment(ref _getInfoCalls);

                    return PropertyOrFieldServiceInfo.Of(member);
                }
                else
                {
                    return null;
                }
            }
        }

    }
}
