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

        private void RegisterServices(IContainer container, IReuse reuse)
        {
            container.Register<MyServiceA>(reuse);
            container.Register<MyServiceB>(reuse);

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

        public void Dispose()
        {
            scopeContextFactory.Dispose();
        }

        public class MyServiceA : IDisposable
        {
            public static int DisposedCount;

            MyServiceB _service2;
            public CustomValue CustomValue { get; private set; }

            public Core.IServiceResolver Resolver { get; private set; }

            public MyServiceA(MyServiceB service2, Core.IServiceResolver resolver, CustomValue value)
            {
                _service2 = service2;
                CustomValue = value;
                this.Resolver = resolver;
            }

            public void DoStuff()
            {
                _service2.DoStuff();
            }

            public void Dispose()
            {
                DisposedCount++;
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
