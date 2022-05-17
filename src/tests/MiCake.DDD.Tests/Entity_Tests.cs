using MiCake.Cord.Tests.Fakes.DomainEvents;
using MiCake.Cord.Tests.Fakes.Entities;
using System;
using Xunit;

namespace MiCake.Cord.Tests
{
    public class Entity_Tests
    {
        [Fact]
        public void InheritEntityCompare()
        {
            var entityA = new EntityA() { Id = 1 };
            var inheritEntity = new ClassAInheritEnityA() { Id = 1 };

            Assert.Equal(entityA, inheritEntity);
        }

        [Fact]
        public void Entity_Operator()
        {
            var entityA = new EntityA() { Id = 1 };
            var inheritEntity = new ClassAInheritEnityA() { Id = 1 };

            var compareResult = entityA == inheritEntity;
            Assert.True(compareResult);

            var inheritEntity2 = new ClassAInheritEnityA() { Id = 2 };
            var compareIsFalse = entityA != inheritEntity2;
            Assert.True(compareIsFalse);
        }

        [Fact]
        public void Entity_Equal_Test()
        {
            var entityA = new EntityA() { Id = 1 };
            var inheritEntity = new ClassAInheritEnityA();

            var guid = Guid.NewGuid();
            var genericEntityA = new GenericEntityA() { Id = guid };
            var genericEntityB = new GenericEntityB() { Id = guid };

            //equal to null
            Assert.False(entityA.Equals(null));

            //not reference
            Assert.False(entityA.Equals(new object()));

            //one has id ,other one id is default
            Assert.False(inheritEntity.Equals(entityA));

            //not AssignableFrom but id is same.
            Assert.False(genericEntityA.Equals(genericEntityB));

            var defaultEntityA = new GenericEntityA();
            var defaultEntityB = new GenericEntityB();

            //two entity is default id
            Assert.False(defaultEntityA.Equals(defaultEntityB));
        }

        [Fact]
        public void Entity_GetHashCode_Test()
        {
            var entityA = new EntityA() { Id = 1 };
            Assert.Equal(1, entityA.GetHashCode());

            var defaultEntityA = new GenericEntityA();
            Assert.Equal(0, defaultEntityA.GetHashCode());
        }

        [Fact]
        public void Entity_DomainEvent_Test()
        {
            var entityA = new EntityA() { Id = 1 };
            var eventInfo = new CreateOrderEvents(1);
            entityA.AddDomainEvent(eventInfo);

            Assert.Single(entityA.GetDomainEvents());

            entityA.RemoveDomainEvent(eventInfo);

            Assert.Empty(entityA.GetDomainEvents());
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
