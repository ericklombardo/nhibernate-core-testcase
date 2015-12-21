using System;

namespace NHibernate.Test.NHSpecificTest.NH3841
{
    public class ChildEntity
    {
        public virtual Guid Id { get; set; }
        public virtual string Name { get; set; }
        public virtual ParentEntity ParentEntity { get; set; } 
    }
}