using MiCake.DDD.Domain;
using MiCake.DDD.Tests.Fakes.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace MiCake.DDD.Tests
{
    public class Entity_Compare_Tests
    {
        [Fact]
        public void InheritEntityCompare()
        {
            var entityA = new EntityA() { Id = 1 };
            var inheritEntity = new ClassAInheritEnityA() { Id = 1 };

            Assert.Equal(entityA, inheritEntity);
        }

        [Fact]
        public void GenericEntity_SameType_Test()
        {
            var entityA = new GenericEntityA() { Id = Guid.NewGuid() };
            var inheritEntity = new ClassAInheritGenericEntityA() { Id = entityA.Id };

            Assert.Equal(entityA, inheritEntity);
        }
    }
}
