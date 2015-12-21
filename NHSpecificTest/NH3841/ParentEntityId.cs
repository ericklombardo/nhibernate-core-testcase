namespace NHibernate.Test.NHSpecificTest.NH3841
{
    public class ParentEntityId
    {
        public virtual string Prop1 { get; set; }
        public virtual string Prop2 { get; set; }

        protected bool Equals(ParentEntityId other)
        {
            return string.Equals(Prop1, other.Prop1) && string.Equals(Prop2, other.Prop2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ParentEntityId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Prop1.GetHashCode()*397) ^ Prop2.GetHashCode();
            }
        }

        public static bool operator ==(ParentEntityId left, ParentEntityId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ParentEntityId left, ParentEntityId right)
        {
            return !Equals(left, right);
        }
    }
}