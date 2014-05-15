using System;
using System.Diagnostics;
using System.Reflection;
using System.Web.Configuration;
using System.Web.Hosting;

namespace TcmDevelopment.VirtualPathProvider
{
	/// <summary>
	/// <see cref="ConfigMapPath" /> hooks the MapPath functionality in ASP.NET and redirects file mapping actions as per the 
	/// TcmDevelopment configuration
	/// </summary>
	public class ConfigMapPath : IConfigMapPath
	{
		private VirtualPathProvider mProvider;
		private IConfigMapPath mOriginal;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigMapPath"/> class.
		/// </summary>
		/// <param name="virtualPathProvider"><see cref="T:TcmDevelopment.VirtualPathProvider.VirtualPathProvider" /></param>
		public ConfigMapPath(VirtualPathProvider virtualPathProvider)
		{
			mProvider = virtualPathProvider;

			// Initialize the custom ConfigMapPath hook
			HookMapPath();
		}

		/// <summary>
		/// Hook the internal .NET MapPath function in order to change its behaviour to handle virtual paths
		/// </summary>
		/// <exception cref="System.ArgumentNullException">
		/// _theHostingEnvironment is null
		/// or
		/// _configMapPath is null
		/// </exception>
		private void HookMapPath()
		{
			FieldInfo fieldInstance = typeof(HostingEnvironment).GetField("_theHostingEnvironment", BindingFlags.NonPublic | BindingFlags.Static);
			FieldInfo fieldConfigMapPath = typeof(HostingEnvironment).GetField("_configMapPath", BindingFlags.NonPublic | BindingFlags.Instance);
			FieldInfo fieldConfigMapPath2 = typeof(HostingEnvironment).GetField("_configMapPath2", BindingFlags.NonPublic | BindingFlags.Instance);

			// Obtain the internal hosting environment instance (_theHostingEnvironment)
			HostingEnvironment hostingEnvironmentInstance = (HostingEnvironment)fieldInstance.GetValue(null);

			if (hostingEnvironmentInstance == null)
				throw new ArgumentNullException("_theHostingEnvironment is null");

			// Obtain the instantiated IConfigMapPath interface object
			mOriginal = (IConfigMapPath)fieldConfigMapPath.GetValue(hostingEnvironmentInstance);

			if (mOriginal == null)
				throw new ArgumentNullException("_configMapPath is null");

			// Replace the HostingEnvironment IConfigMapPath object with our own
			fieldConfigMapPath.SetValue(hostingEnvironmentInstance, this);

			// Disable any IIS specific IConfigMapPath2 object
			fieldConfigMapPath2.SetValue(hostingEnvironmentInstance, null);
		}

		/// <summary>
		/// Gets the virtual-directory name associated with a specific site.
		/// </summary>
		/// <param name="siteID">A unique identifier for the site.</param>
		/// <param name="path">The URL associated with the site.</param>
		/// <returns>
		/// The <paramref name="siteID" /> must be unique. No two sites share the same id. The <paramref name="siteID" /> distinguishes sites that have the same name.
		/// </returns>
		public String GetAppPathForPath(String siteID, String path)
		{
			return mOriginal.GetAppPathForPath(siteID, path);
		}

		/// <summary>
		/// Populates the default site name and the site ID.
		/// </summary>
		/// <param name="siteName">The default site name.</param>
		/// <param name="siteID">A unique identifier for the site.</param>
		public void GetDefaultSiteNameAndID(out String siteName, out String siteID)
		{
			mOriginal.GetDefaultSiteNameAndID(out siteName, out siteID);
		}

		/// <summary>
		/// Gets the machine-configuration file name.
		/// </summary>
		/// <returns>
		/// The machine-configuration file name.
		/// </returns>
		public String GetMachineConfigFilename()
		{
			return mOriginal.GetMachineConfigFilename();
		}

		/// <summary>
		/// Populates the directory and name of the configuration file based on the site ID and site path.
		/// </summary>
		/// <param name="siteID">A unique identifier for the site.</param>
		/// <param name="path">The URL associated with the site.</param>
		/// <param name="directory">The physical directory of the configuration path.</param>
		/// <param name="baseName">The name of the configuration file.</param>
		public void GetPathConfigFilename(String siteID, String path, out String directory, out String baseName)
		{
			mOriginal.GetPathConfigFilename(siteID, path, out directory, out baseName);
		}

		/// <summary>
		/// Gets the name of the configuration file at the Web root.
		/// </summary>
		/// <returns>
		/// The name of the configuration file at the Web root.
		/// </returns>
		public String GetRootWebConfigFilename()
		{
			return mOriginal.GetRootWebConfigFilename();
		}

		/// <summary>
		/// Gets the physical directory path based on the site ID and URL associated with the site.
		/// </summary>
		/// <param name="siteID">A unique identifier for the site.</param>
		/// <param name="path">The URL associated with the site.</param>
		/// <returns>
		/// The physical directory path.
		/// </returns>
		public String MapPath(String siteID, String path)
		{
			String mappedPath = mProvider.IsPathVirtual(path);

			#if VPP_DEBUG
			Debug.WriteLine(String.Format("TcmDevelopment: MapPath \"{0}\".", path));
			#endif

			if (!String.IsNullOrEmpty(mappedPath))
				return mappedPath;

			return mOriginal.MapPath(siteID, path);
		}

		/// <summary>
		/// Populates the site name and site ID based on a site argument value.
		/// </summary>
		/// <param name="siteArgument">The site name or site identifier.</param>
		/// <param name="siteName">The default site name.</param>
		/// <param name="siteID">A unique identifier for the site.</param>
		public void ResolveSiteArgument(String siteArgument, out String siteName, out String siteID)
		{
			mOriginal.ResolveSiteArgument(siteArgument, out siteName, out siteID);
		}
	}
}
