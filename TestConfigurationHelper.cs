namespace NHibernate.Test
{
	using System;
	using System.IO;
	using Cfg;

	public static class TestConfigurationHelper
	{
		public static readonly string HibernateConfigFile;

		static TestConfigurationHelper()
		{
			// Verify if hibernate.Cfg.xml exists
			HibernateConfigFile = GetDefaultConfigurationFilePath();
		}

		public static string GetDefaultConfigurationFilePath()
		{
			string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			string relativeSearchPath = AppDomain.CurrentDomain.RelativeSearchPath;
			string binPath = relativeSearchPath == null ? baseDir : Path.Combine(baseDir, relativeSearchPath);
			string fullPath = Path.Combine(binPath, Configuration.DefaultHibernateCfgFileName);
			return File.Exists(fullPath) ? fullPath : null;
		}

		/// <summary>
		/// Standar Configuration for tests.
		/// </summary>
		/// <returns>The configuration using merge between App.Config and hibernate.Cfg.xml if present.</returns>
		public static Configuration GetDefaultConfiguration()
		{
			Configuration result = new Configuration();
			if (HibernateConfigFile != null)
				result.Configure(HibernateConfigFile);
			return result;
		}
	}
}