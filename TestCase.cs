using System;
using System.Collections;
using System.Data;
using System.Reflection;
using log4net;
using log4net.Config;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Engine;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Type;
using NUnit.Framework;
using NHibernate.Hql.Ast.ANTLR;

namespace NHibernate.Test
{
    public abstract class TestCase
    {
        private const bool OutputDdl = false;
        protected Configuration Cfg;
        protected ISessionFactoryImplementor Sessions;

        private static readonly ILog Log = LogManager.GetLogger(typeof(TestCase));

        protected Dialect.Dialect Dialect => NHibernate.Dialect.Dialect.GetDialect(Cfg.Properties);

        protected TestDialect TestDialect => TestDialect.GetTestDialect(Dialect);


        /// <summary>
        /// To use in in-line test
        /// </summary>
        protected bool IsAntlrParser => Sessions.Settings.QueryTranslatorFactory is ASTQueryTranslatorFactory;

        protected ISession LastOpenedSession;
        private DebugConnectionProvider _connectionProvider;

        /// <summary>
        /// Mapping files used in the TestCase
        /// </summary>
        protected abstract IList Mappings { get; }

        /// <summary>
        /// Assembly to load mapping files from (default is NHibernate.DomainModel).
        /// </summary>
        protected virtual string MappingsAssembly => "NHibernate.DomainModel";

        static TestCase()
        {
            // Configure log4net here since configuration through an attribute doesn't always work.
            XmlConfigurator.Configure();
        }

        /// <summary>
        /// Creates the tables used in this TestCase
        /// </summary>
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            try
            {
                Configure();
                if (!AppliesTo(Dialect))
                {
                    Assert.Ignore(GetType() + " does not apply to " + Dialect);
                }

                CreateSchema();
                try
                {
                    BuildSessionFactory();
                    if (!AppliesTo(Sessions))
                    {
                        Assert.Ignore(GetType() + " does not apply with the current session-factory configuration");
                    }
                }
                catch
                {
                    DropSchema();
                    throw;
                }
            }
            catch (Exception e)
            {
                Cleanup();
                Log.Error("Error while setting up the test fixture", e);
                throw;
            }
        }

        /// <summary>
        /// Removes the tables used in this TestCase.
        /// </summary>
        /// <remarks>
        /// If the tables are not cleaned up sometimes SchemaExport runs into
        /// Sql errors because it can't drop tables because of the FKs.  This 
        /// will occur if the TestCase does not have the same hbm.xml files
        /// included as a previous one.
        /// </remarks>
        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            if (!AppliesTo(Dialect))
                return;

            DropSchema();
            Cleanup();
        }

        protected virtual void OnSetUp()
        {
        }

        /// <summary>
        /// Set up the test. This method is not overridable, but it calls
        /// <see cref="OnSetUp" /> which is.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            OnSetUp();
        }

        protected virtual void OnTearDown()
        {
        }

        /// <summary>
        /// Checks that the test case cleans up after itself. This method
        /// is not overridable, but it calls <see cref="OnTearDown" /> which is.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            OnTearDown();

            bool wasClosed = CheckSessionWasClosed();
            bool wasCleaned = CheckDatabaseWasCleaned();
            bool wereConnectionsClosed = CheckConnectionsWereClosed();
            bool fail = !wasClosed || !wasCleaned || !wereConnectionsClosed;

            if (fail)
            {
                Assert.Fail("Test didn't clean up after itself. session closed: " + wasClosed + " database cleaned: " + wasCleaned
                    + " connection closed: " + wereConnectionsClosed);
            }
        }

        private bool CheckSessionWasClosed()
        {
            if (LastOpenedSession != null && LastOpenedSession.IsOpen)
            {
                Log.Error("Test case didn't close a session, closing");
                LastOpenedSession.Close();
                return false;
            }

            return true;
        }

        private bool CheckDatabaseWasCleaned()
        {
            if (Sessions.GetAllClassMetadata().Count == 0)
            {
                // Return early in the case of no mappings, also avoiding
                // a warning when executing the HQL below.
                return true;
            }

            bool empty;
            using (ISession s = Sessions.OpenSession())
            {
                IList objects = s.CreateQuery("from System.Object o").List();
                empty = objects.Count == 0;
            }

            if (!empty)
            {
                Log.Error("Test case didn't clean up the database after itself, re-creating the schema");
                DropSchema();
                CreateSchema();
            }

            return empty;
        }

        private bool CheckConnectionsWereClosed()
        {
            if (_connectionProvider == null || !_connectionProvider.HasOpenConnections)
            {
                return true;
            }

            Log.Error("Test case didn't close all open connections, closing");
            _connectionProvider.CloseAllConnections();
            return false;
        }

        private void Configure()
        {
            Cfg = new Configuration();
            if (TestConfigurationHelper.HibernateConfigFile != null)
                Cfg.Configure(TestConfigurationHelper.HibernateConfigFile);

            AddMappings(Cfg);

            Configure(Cfg);

            ApplyCacheSettings(Cfg);
        }

        protected virtual void AddMappings(Configuration configuration)
        {
            Assembly assembly = Assembly.Load(MappingsAssembly);

            foreach (string file in Mappings)
            {
                configuration.AddResource(MappingsAssembly + "." + file, assembly);
            }
        }

        protected virtual void CreateSchema()
        {
            new SchemaExport(Cfg).Create(OutputDdl, true);
        }

        private void DropSchema()
        {
            new SchemaExport(Cfg).Drop(OutputDdl, true);
        }

        protected virtual void BuildSessionFactory()
        {
            Sessions = (ISessionFactoryImplementor)Cfg.BuildSessionFactory();
            _connectionProvider = Sessions.ConnectionProvider as DebugConnectionProvider;
        }

        private void Cleanup()
        {
            Sessions?.Close();
            Sessions = null;
            _connectionProvider = null;
            LastOpenedSession = null;
            Cfg = null;
        }

        public int ExecuteStatement(string sql)
        {
            if (Cfg == null)
            {
                Cfg = new Configuration();
            }

            using (IConnectionProvider prov = ConnectionProviderFactory.NewConnectionProvider(Cfg.Properties))
            {
                IDbConnection conn = prov.GetConnection();

                try
                {
                    using (IDbTransaction tran = conn.BeginTransaction())
                    using (IDbCommand comm = conn.CreateCommand())
                    {
                        comm.CommandText = sql;
                        comm.Transaction = tran;
                        comm.CommandType = CommandType.Text;
                        int result = comm.ExecuteNonQuery();
                        tran.Commit();
                        return result;
                    }
                }
                finally
                {
                    prov.CloseConnection(conn);
                }
            }
        }

        public int ExecuteStatement(ISession session, ITransaction transaction, string sql)
        {
            using (IDbCommand cmd = session.Connection.CreateCommand())
            {
                cmd.CommandText = sql;
                transaction?.Enlist(cmd);
                return cmd.ExecuteNonQuery();
            }
        }

        protected ISessionFactoryImplementor Sfi => Sessions;

        protected virtual ISession OpenSession()
        {
            LastOpenedSession = Sessions.OpenSession();
            return LastOpenedSession;
        }

        protected virtual ISession OpenSession(IInterceptor sessionLocalInterceptor)
        {
            LastOpenedSession = Sessions.OpenSession(sessionLocalInterceptor);
            return LastOpenedSession;
        }

        protected void ApplyCacheSettings(Configuration configuration)
        {
            if (CacheConcurrencyStrategy == null)
            {
                return;
            }

            foreach (PersistentClass clazz in configuration.ClassMappings)
            {
                bool hasLob = false;
                foreach (Property prop in clazz.PropertyClosureIterator)
                {
                    if (prop.Value.IsSimpleValue)
                    {
                        IType type = ((SimpleValue)prop.Value).Type;
                        if (Equals(type, NHibernateUtil.BinaryBlob))
                        {
                            hasLob = true;
                        }
                    }
                }
                if (!hasLob && !clazz.IsInherited)
                {
                    configuration.SetCacheConcurrencyStrategy(clazz.EntityName, CacheConcurrencyStrategy);
                }
            }

            foreach (Mapping.Collection coll in configuration.CollectionMappings)
            {
                configuration.SetCollectionCacheConcurrencyStrategy(coll.Role, CacheConcurrencyStrategy);
            }
        }

        #region Properties overridable by subclasses

        protected virtual bool AppliesTo(Dialect.Dialect dialect)
        {
            return true;
        }

        protected virtual bool AppliesTo(ISessionFactoryImplementor factory)
        {
            return true;
        }

        protected virtual void Configure(Configuration configuration)
        {
        }

        protected virtual string CacheConcurrencyStrategy => "nonstrict-read-write";

        #endregion
    }
}
