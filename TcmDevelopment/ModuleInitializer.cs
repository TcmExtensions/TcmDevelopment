#region Header
////////////////////////////////////////////////////////////////////////////////////
//
//	File Description: Initializer
// ---------------------------------------------------------------------------------
//	Date Created	: March 4, 2013
//	Author			: Rob van Oostenrijk
// ---------------------------------------------------------------------------------
// 	Change History
//	Date Modified       : 
//	Changed By          : 
//	Change Description  : 
//
////////////////////////////////////////////////////////////////////////////////////
#endregion
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.Hosting;
using System.Xml.Linq;

namespace TcmDevelopment
{
	/// <summary>
	/// <see cref="ModuleInitializer" /> is automatically executed by ASP.NET on application pool startup.
	/// </summary>
	public static class ModuleInitializer
	{
		private const String TRIDION_ASSEMBLY = "Tridion.ContentDelivery.Configuration";
		private const String TRIDION_HOOK = "Tridion.ContentDelivery.Web.Jvm.ConfigurationHook";
		private const String TRIDION_FIELD = "configFolder";

		private static void SetConfigurationHook(String configurationFolder)
		{
			if (String.IsNullOrEmpty(configurationFolder) || !Directory.Exists(configurationFolder))
				return;

			Assembly tridionConfig = null;

			Debug.WriteLine("TcmDevelopment: Application configuration hook for \"{0}\".", configurationFolder);

			try
			{
				// Use deprecated LoadWithPartialName because it allows partial binding in order to prevent version dependencies
				tridionConfig = Assembly.LoadWithPartialName(TRIDION_ASSEMBLY);
			}
			catch
			{
			}

			if (tridionConfig == null)
			{
				Debug.WriteLine("TcmDevelopment: Failed to load \"{0}.dll\".", TRIDION_ASSEMBLY);
				return;
			}

			Type configurationHook = tridionConfig.GetType(TRIDION_HOOK, false, true);

			if (configurationHook == null)
			{
				Debug.WriteLine("TcmDevelopment: Failed to get type \"{0}\".", TRIDION_HOOK);
				return;
			}

			FieldInfo configFolderField = configurationHook.GetField(TRIDION_FIELD, BindingFlags.Public | BindingFlags.Static);

			if (configFolderField == null)
			{
				Debug.WriteLine("TcmDevelopment: Failed to get field \"{0}\".", TRIDION_FIELD);
				return;
			}

			configFolderField.SetValue(null, configurationFolder);
		}

		/// <summary>
		/// Retrieve TcmDevelopment configuration file
		/// </summary>
		/// <value>
		/// Configuration as <see cref="System.Xml.Linq.XElement" /> or null
		/// </value>
		private static XElement Configuration
		{
			get
			{
				try
				{
					String configurationPath = HostingEnvironment.MapPath("~/config/tcmdevelopment.config");

					if (File.Exists(configurationPath))
						return XElement.Load(configurationPath);

					configurationPath = HostingEnvironment.MapPath("~/tcmdevelopment.config");

					if (File.Exists(configurationPath))
						return XElement.Load(configurationPath);
				}
				catch (Exception ex)
				{
					throw new Exception("TcmDevelopment: Unable to find or load configuration file tcmdevelopment.config", ex);
				}

				throw new Exception("TcmDevelopment: Unable to find or load configuration file tcmdevelopment.config");
			}
		}

		/// <summary>
		/// Determines whether <see cref="P:tridionPath" /> is a valid Tridion home directory
		/// </summary>
		/// <param name="tridionPath">Path to a tridion installation</param>
		/// <returns><c>true</c> if a valid other; otherwise <c>false</c></returns>
		private static Boolean IsValidTridionHome(String tridionPath)
		{
			if (String.IsNullOrEmpty(tridionPath) || !Directory.Exists(tridionPath))
				return false;

			bool hasCore = File.Exists(Path.Combine(tridionPath, @"lib\cd_core.jar"));
			bool hasModel = File.Exists(Path.Combine(tridionPath, @"lib\cd_model.jar"));

			if (!hasCore || !hasModel)
				return false;

			bool hasBroker = File.Exists(Path.Combine(tridionPath, @"config\cd_broker_conf.xml"));
			bool hasStorage = File.Exists(Path.Combine(tridionPath, @"config\cd_storage_conf.xml"));
			
			if (!hasBroker && !hasStorage)
				return false;

			return true;
		}

		private static void InitializeTridion()
		{
			XElement config = Configuration;

			if (config != null)
			{
				String mode = config.Attribute("mode").Value;
				String tridionPath = config.Attribute("tridion").Value;

				if (!Directory.Exists(tridionPath) && !IsValidTridionHome(tridionPath))
					throw new Exception(String.Format("TcmDevelopment: Configured tridion location \"{0}\" is invalid.", tridionPath));

				String configurationPath = Path.Combine(tridionPath, "config", mode);

				Debug.WriteLine("TcmDevelopment: Initializing for mode \"{0}\".", mode);
				Debug.WriteLine("TcmDevelopment: Tridion location \"{0}\".", tridionPath);
				Debug.WriteLine("TcmDevelopment: Configuration location \"{0}\".", configurationPath);

				// Modify Tridion folder and configuration location according to requested environment
				Environment.SetEnvironmentVariable("TRIDION_HOME", tridionPath);
				SetConfigurationHook(configurationPath);

				// Modify the current directory to the Tridion configuration path
				Environment.CurrentDirectory = configurationPath;

				// Set managable heap sizes for CodeMesh JuggerNET
				// http://codemesh.com/products/juggernet/doc/runtime_config.html
				Environment.SetEnvironmentVariable("XMOG_INITIAL_HEAPSIZE", "16");
				Environment.SetEnvironmentVariable("XMOG_MAXIMUM_HEAPSIZE", "64");
			}
		}

		/// <summary>
		/// Initializes the VirtualPathProvider
		/// </summary>
		/// <exception cref="System.Exception">
		/// </exception>
		private static void InitializeVirtualPathProvider(bool remapFiles)
		{
			XElement config = Configuration;

			if (config != null)
			{
				Debug.WriteLine("TcmDevelopment: Registering TcmDevelopment Virtual Path Provider");
				HostingEnvironment.RegisterVirtualPathProvider(new VirtualPathProvider.VirtualPathProvider(config, remapFiles));
			}
		}
			
		/// <summary>
		/// Initializer entrypoint
		/// </summary>
		public static void Initialize()
		{
			// Do not initialize when executing in the build environment
			if (HostingEnvironment.InClientBuildManager)
				return;
			
			Debug.WriteLine("TcmDevelopment: Initializing Library");

			String process = Process.GetCurrentProcess().ProcessName;

			// For ASP.NET development webserver hosting we load Tridion and the virtual path provider
			if (process.StartsWith("WebDev.WebServer", StringComparison.OrdinalIgnoreCase))
			{
				InitializeVirtualPathProvider(true);
				InitializeTridion();
			}

			// For IIS Express hosting we load Tridion
			if (process.StartsWith("iisexpress", StringComparison.OrdinalIgnoreCase))
			{
				InitializeVirtualPathProvider(false);
				InitializeTridion();
			}
		}
	}
}

