#region Header
////////////////////////////////////////////////////////////////////////////////////
//
//	File Description: Virtual Path Provider
// ---------------------------------------------------------------------------------
//	Date Created	: March 4, 2013
//	Author			: Rob van Oostenrijk
// ---------------------------------------------------------------------------------
// 	Change History
//	Date Modified       : April 25, 2013
//	Changed By          : Rob van Oostenrijk
//	Change Description  : Added support for local files in order to override files on the webserver.
//
////////////////////////////////////////////////////////////////////////////////////
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Xml.Linq;

namespace TcmDevelopment.VirtualPathProvider
{
    public class VirtualPathProvider : System.Web.Hosting.VirtualPathProvider
    {
		private static Regex mFolderMatch = new Regex(@"^(?<folder>~/.*?)/(?<subfolder>.*?/)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private ConfigMapPath mConfigMapPath;
        private Dictionary<String, String> mMappings;        
		private String mBasePath;

		private bool mRemapFiles = true;

		private static String AppendSlash(String input)
		{
			if (input.EndsWith(@"\")) 
				return input; 
			else
				return input + @"\"; 
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualPathProvider"/> class.
        /// </summary>
        public VirtualPathProvider(XElement configuration)
        {
			// Load our virtual path mappings
			LoadMappingConfiguration(configuration);

			mConfigMapPath = new ConfigMapPath(this);
        }

		public VirtualPathProvider(XElement configuration, bool remapFiles): this(configuration)
		{
			mRemapFiles = remapFiles;
		}

        /// <summary>
        /// Loads the mapping configuration.
        /// </summary>
        /// <exception cref="System.Exception">DevelopmentVPP, configuration file development_vpp.config does not exist.</exception>
        private void LoadMappingConfiguration(XElement configuration)
        {
			String mode = configuration.Attribute("mode").Value;

			XAttribute xbasePath = configuration.Attribute("basePath");

			if (xbasePath != null)
			{
				String basePath = configuration.Attribute("basePath").Value;

				if (!String.IsNullOrEmpty(basePath))
				{
					if (!Directory.Exists(basePath))
					{
						try
						{
							Directory.CreateDirectory(basePath);
						}
						catch
						{
						}
					}

					if (Directory.Exists(basePath))
					{
						mBasePath = AppendSlash(Path.GetDirectoryName(AppendSlash(basePath)));
						Debug.WriteLine(String.Format("TcmDevelopment: Virtual path provider basePath \"{0}\".", mBasePath));
					}
				}
			}
			
            // Load the mappings into a case-insensitivy Dictionary for quick lookup
            mMappings = configuration.Elements("path").Where(x => x.Attribute("mode").Value == mode).ToDictionary(
                    data => data.Attribute("virtual").Value,
                    data => data.Attribute("location").Value, StringComparer.OrdinalIgnoreCase);
        }

        private String LocateMapping(String relativePath, String Prefix)
        {
            String mapping;

            // Try to match on "folder/subfolder" first
            if (mMappings.TryGetValue(Prefix, out mapping))
            {
                // Mapping has been located, rebuild the virtual URL to an absolute file location
				String location = mapping + relativePath.Substring(Prefix.Length).Replace("/", "\\");

				#if VPP_DEBUG
				Debug.WriteLine(String.Format("TcmDevelopment: Mapping \"{0}\" for prefix \"{1}\" to \"{2}\".", relativePath, Prefix, location));
				#endif

				return location;
            }

            return String.Empty;
        }

        /// <summary>
        /// Determines whether the specified path is a virtual mapped directory.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <returns>The physical path it is mapped to in case of a virtual mapping.</returns>
        public String IsPathVirtual(String virtualPath)
        {
            if (mMappings != null)
            {
                String relativePath = VirtualPathUtility.ToAppRelative(virtualPath);

				if (!String.IsNullOrEmpty(mBasePath))
				{
					String physicalRelative = VirtualPathUtility.ToAbsolute(virtualPath, HostingEnvironment.ApplicationVirtualPath).Replace("/", @"\");
					String physicalPath = Path.Combine(mBasePath, physicalRelative.Substring(1));

					if (File.Exists(physicalPath))
					{
						#if VPP_DEBUG
						Debug.WriteLine(String.Format("TcmDevelopment: Physical path \"{0}\".", physicalPath));
						#endif

						return physicalPath;
					}
				}

				// If we are not asked to remap files, stop after checking physical file location
				if (!mRemapFiles)
					return String.Empty;

                Match match = mFolderMatch.Match(VirtualPathUtility.AppendTrailingSlash(relativePath));

                if (match.Success)
                {
                    String mapping;
                    String folder = match.Groups["folder"].Value;                    
                    String subfolder = VirtualPathUtility.RemoveTrailingSlash(match.Groups["subfolder"].Value);

                    mapping = LocateMapping(relativePath, folder + "/" + subfolder);

                    if (!String.IsNullOrEmpty(mapping))
                        return mapping;

                    mapping = LocateMapping(relativePath, folder);

                    if (!String.IsNullOrEmpty(mapping))
                        return mapping;
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Gets a virtual directory from the virtual file system.
        /// </summary>
        /// <param name="virtualDir">The path to the virtual directory.</param>
        /// <returns>
        /// A descendent of the <see cref="T:System.Web.Hosting.VirtualDirectory" /> class that represents a directory in the virtual file system.
        /// </returns>
        public override System.Web.Hosting.VirtualDirectory GetDirectory(String virtualDir)
        {
			String mappedPath = IsPathVirtual(virtualDir);

			if (!String.IsNullOrEmpty(mappedPath))
            {
                #if VPP_DEBUG
				Debug.WriteLine(String.Format("TcmDevelopment: GetDirectory \"{0}\".", mappedPath));
                #endif

                return base.GetDirectory(virtualDir);
            }
            else
                return Previous.GetDirectory(virtualDir);
        }

        /// <summary>
        /// Gets a virtual file from the virtual file system.
        /// </summary>
        /// <param name="virtualPath">The path to the virtual file.</param>
        /// <returns>
        /// A descendent of the <see cref="T:System.Web.Hosting.VirtualFile" /> class that represents a file in the virtual file system.
        /// </returns>
        public override System.Web.Hosting.VirtualFile GetFile(String virtualPath)
        {
            String mappedPath = IsPathVirtual(virtualPath);

            if (!String.IsNullOrEmpty(mappedPath))
            {
                #if VPP_DEBUG
				Debug.WriteLine(String.Format("TcmDevelopment: GetFile \"{0}\".", mappedPath));
                #endif

                return new VirtualFile(virtualPath, mappedPath);                
            }
            else
                return Previous.GetFile(virtualPath);
        }

        /// <summary>
        /// Gets a value that indicates whether a directory exists in the virtual file system.
        /// </summary>
        /// <param name="virtualDir">The path to the virtual directory.</param>
        /// <returns>
        /// true if the directory exists in the virtual file system; otherwise, false.
        /// </returns>
		public override bool DirectoryExists(String virtualPath)
        {
			bool result = Previous.DirectoryExists(virtualPath);

            #if VPP_DEBUG
			Debug.WriteLine(String.Format("TcmDevelopment: DirectoryExists \"{0}\" : {1}.", virtualPath, result));
            #endif

            return result;
        }

        /// <summary>
        /// Gets a value that indicates whether a file exists in the virtual file system.
        /// </summary>
        /// <param name="virtualPath">The path to the virtual file.</param>
        /// <returns>
        /// true if the file exists in the virtual file system; otherwise, false.
        /// </returns>
        public override bool FileExists(String virtualPath)
        {
			String mappedPath = IsPathVirtual(virtualPath);

			if (!String.IsNullOrEmpty(mappedPath) && !mRemapFiles)
				return true;

            bool result = Previous.FileExists(virtualPath);

            #if VPP_DEBUG
			Debug.WriteLine(String.Format("TcmDevelopment: DirectoryExists \"{0}\" : {1}.", virtualPath, result));
            #endif

            return result;
        }

        /// <summary>
        /// Returns a hash of the specified virtual paths.
        /// </summary>
        /// <param name="virtualPath">The path to the primary virtual resource.</param>
        /// <param name="virtualPathDependencies">An array of paths to other virtual resources required by the primary virtual resource.</param>
        /// <returns>
        /// A hash of the specified virtual paths.
        /// </returns>
        public override String GetFileHash(String virtualPath, IEnumerable virtualPathDependencies)
        {
			String mappedPath = IsPathVirtual(virtualPath);

			if (!String.IsNullOrEmpty(mappedPath) && !mRemapFiles)
				return null;

            String result = Previous.GetFileHash(virtualPath, virtualPathDependencies);

            #if VPP_DEBUG
			Debug.WriteLine(String.Format("TcmDevelopment: GetFileHash \"{0}\" : {1}.", virtualPath, result));
            #endif

            return result;
        }

        /// <summary>
        /// Creates a cache dependency based on the specified virtual paths.
        /// </summary>
        /// <param name="virtualPath">The path to the primary virtual resource.</param>
        /// <param name="virtualPathDependencies">An array of paths to other resources required by the primary virtual resource.</param>
        /// <param name="utcStart">The UTC time at which the virtual resources were read.</param>
        /// <returns>
        /// A <see cref="T:System.Web.Caching.CacheDependency" /> object for the specified virtual resources.
        /// </returns>
        public override CacheDependency GetCacheDependency(String virtualPath, System.Collections.IEnumerable virtualPathDependencies, DateTime utcStart)
        {
			String mappedPath = IsPathVirtual(virtualPath);

			if (!String.IsNullOrEmpty(mappedPath) && !mRemapFiles)
				return null;

            CacheDependency result = Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);

            #if VPP_DEBUG
			Debug.WriteLine(String.Format("TcmDevelopment: GetCacheDependency \"{0}\" : {1}.", virtualPath, result != null));
            #endif

            return result;
        }

        /// <summary>
        /// Combines a base path with a relative path to return a complete path to a virtual resource.
        /// </summary>
        /// <param name="basePath">The base path for the application.</param>
        /// <param name="relativePath">The path to the virtual resource, relative to the base path.</param>
        /// <returns>
        /// The complete path to a virtual resource.
        /// </returns>
        public override String CombineVirtualPaths(String basePath, String relativePath)
        {
            String result = Previous.CombineVirtualPaths(basePath, relativePath);

            #if VPP_DEBUG
			Debug.WriteLine(String.Format("TcmDevelopment: CombineVirtualPaths \"{0}\", \"{1}\": \"{2}\".", basePath, relativePath, result));
            #endif

            return result;
        }

        /// <summary>
        /// Returns a cache key to use for the specified virtual path.
        /// </summary>
        /// <param name="virtualPath">The path to the virtual resource.</param>
        /// <returns>
        /// A cache key for the specified virtual resource.
        /// </returns>
        public override String GetCacheKey(String virtualPath)
        {
			String mappedPath = IsPathVirtual(virtualPath);

			if (!String.IsNullOrEmpty(mappedPath) && !mRemapFiles)
				return null;

            String result = Previous.GetCacheKey(virtualPath);

            #if VPP_DEBUG
			Debug.WriteLine(String.Format("TcmDevelopment: GetCacheKey \"{0}\": \"{1}\".", virtualPath, result));
            #endif

            return result;
        }
    }
}
