using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace NHibernate.Test.NHSpecificTest.NH3841
{
    public class ParentEntityMapping : ClassMapping<ParentEntity>
    {
        public ParentEntityMapping()
        {
            ComponentAsId(x => x.Id, m =>
            {
                m.Property(x => x.Prop1);
                m.Property(x => x.Prop2);
            });
            Property(x => x.Name);
        }
    }

    public class ChildEntityMapping : ClassMapping<ChildEntity>
    {
        public ChildEntityMapping()
        {
            Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            Property(x => x.Name);
            ManyToOne(x => x.ParentEntity, m => m.Columns(
                        c => c.Name("Prop1"),
                        c => c.Name("Prop2")
                    ));
        }
    }

}