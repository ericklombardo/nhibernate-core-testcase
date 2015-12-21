using System.Collections;

namespace NHibernate.Test.NHSpecificTest
{
	/// <summary>
	/// Base class that can be used for tests in NH* subdirectories.
	/// Assumes all mappings are in a single file named <c>Mappings.hbm.xml</c>
	/// in the subdirectory.
	/// </summary>
	public abstract class BugTestCase : TestCase
	{
		protected override string MappingsAssembly => "NHibernate.Test";

	    public virtual string BugNumber
		{
			get
			{
				string ns = GetType().Namespace;
				return ns?.Substring(ns.LastIndexOf('.') + 1);
			}
		}

	    protected override IList Mappings => new[]
	    {
	        "NHSpecificTest." + BugNumber + ".Mappings.hbm.xml"
	    };
	}
}