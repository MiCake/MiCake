using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Store;
using MiCake.DDD.Extensions.Store;

namespace MiCake.EntityFrameworkCore.Tests.Fakes
{
    public class NormalEntity : Entity
    {
        public string Name { get; set; }
    }

    public class NormalAggregateRoot : AggregateRoot
    {
        public string Name { get; set; }
    }

    public class PersistentAggregateRoot : AggregateRootHasPersistentObject<long>
    {
        public ColorValueObject Color { get; private set; }

        public string Name { get; private set; }

        public PersistentAggregateRoot()
        {
        }

        public PersistentAggregateRoot(string name, ColorValueObject color)
        {
            Name = name;
            Color = color;
        }

        public void SetColor(ColorValueObject color) => Color = color;
    }

    public class ColorValueObject : ValueObject
    {
        public int R { get; private set; }

        public int G { get; private set; }

        public int B { get; private set; }

        public ColorValueObject()
        {
        }

        public ColorValueObject(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    public class AggregateRootPOModel : PersistentObject<long, PersistentAggregateRoot, AggregateRootPOModel>
    {
        public int R { get; private set; }
        public int G { get; private set; }
        public int B { get; private set; }
        public string Name { get; private set; }

        public AggregateRootPOModel()
        {
        }

        public AggregateRootPOModel(string name, int r, int g, int b)
        {
            Name = name;
            R = r;
            G = g;
            B = b;
        }

        public override void ConfigureMapping()
        {
            MapConfiger.MapProperty(s => s.Color.R, d => d.R)
                .MapProperty(s => s.Color.G, d => d.G)
                .MapProperty(s => s.Color.B, d => d.B)
                .MapProperty(s => s.Name, d => d.Name);
        }
    }

    public class DemoDomainEvent : IDomainEvent
    {
        public string Name { get; set; }
    }
}
