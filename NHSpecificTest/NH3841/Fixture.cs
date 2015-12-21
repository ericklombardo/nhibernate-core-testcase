using System.Linq;
using NHibernate.Linq;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH3841
{
    [TestFixture]
    public class Fixture : BugTestCase
    {
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