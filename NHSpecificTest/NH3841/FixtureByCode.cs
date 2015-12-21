using System.Linq;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Linq;
using NHibernate.Mapping.ByCode;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH3841
{
    public class FixtureByCode : TestCaseMappingByCode
    {
        protected override HbmMapping GetMappings()
        {
            var mapper = new ModelMapper();
         
            mapper.AddMappings(new[] { typeof(ParentEntityMapping), typeof(ChildEntityMapping) });
            return mapper.CompileMappingForAllExplicitlyAddedEntities();
        }

        protected override void OnSetUp()
        {
            using (var session = OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var e1 = new ParentEntity
                {
                    Id = new ParentEntityId
                    {
                        Prop1 = "A",
                        Prop2 = "B",
                    },
                    Name = "Bob"
                };
                session.Save(e1);

                session.Flush();
                transaction.Commit();
            }
        }

        protected override void OnTearDown()
        {
            using (var session = OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Delete("from System.Object");

                session.Flush();
                transaction.Commit();
            }
        }

        [Test]
        public void MappingCreated()
        {
            using (var session = OpenSession())
            using (session.BeginTransaction())
            {
                var entities = (from e in session.Query<ParentEntity>()
                                where e.Name == "Bob"
                                select e).ToList();

                Assert.AreEqual(1, entities.Count);
            }
        }

    }
}