using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using NHibernate.Connection;

namespace NHibernate.Test
{
	/// <summary>
	/// This connection provider keeps a list of all open _connections,
	/// it is used when testing to check that tests clean up after themselves.
	/// </summary>
	public class DebugConnectionProvider : DriverConnectionProvider
	{
		private readonly ISet<IDbConnection> _connections = new HashSet<IDbConnection>();

		public override IDbConnection GetConnection()
		{
		    try
		    {
		        IDbConnection connection = base.GetConnection();
                _connections.Add(connection);
                return connection;
            }
		    catch (Exception e)
		    {
		        throw new HibernateException("Could not open connection to: " + ConnectionString, e);
		    }
		    
		}

		public override void CloseConnection(IDbConnection conn)
		{
			base.CloseConnection(conn);
			_connections.Remove(conn);
		}

		public bool HasOpenConnections
		{
			get
			{
				// check to see if all _connections that were at one point opened
				// have been closed through the CloseConnection
				// method
				if (_connections.Count == 0)
				{
					// there are no _connections, either none were opened or
					// all of the closings went through CloseConnection.
					return false;
				}
				else
				{
					// Disposing of an ISession does not call CloseConnection (should it???)
					// so a Diposed of ISession will leave an IDbConnection in the list but
					// the IDbConnection will be closed (atleast with MsSql it works this way).
					foreach (IDbConnection conn in _connections)
					{
						if (conn.State != ConnectionState.Closed)
						{
							return true;
						}
					}

					// all of the _connections have been Disposed and were closed that way
					// or they were Closed through the CloseConnection method.
					return false;
				}
			}
		}

		public void CloseAllConnections()
		{
			while (_connections.Count > 0)
			{
				IEnumerator en = _connections.GetEnumerator();
				en.MoveNext();
				CloseConnection(en.Current as IDbConnection);
			}
		}
	}
}