#region Header
////////////////////////////////////////////////////////////////////////////////////
//
//	File Description: VirtualDirectory
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;

namespace TcmDevelopment.VirtualPathProvider
{
    /// <summary>
    /// <see cref="VirtualDirectory" /> exposes a "directory" object in our virtual mapped filesystem
    /// </summary>
    internal class VirtualDirectory : System.Web.Hosting.VirtualDirectory
    {
        private IEnumerable<VirtualDirectory> mDirectories;
		private IEnumerable<VirtualFile> mFiles;
		private IEnumerable<VirtualFileBase> mChildren;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualDirectory"/> class.
        /// </summary>
        /// <param name="virtualPath"><see cref="VirtualDirectory" /> virtual path.</param>
		/// <param name="physicalPath"><see cref="VirtualDirectory" /> physical path.</param>
        public VirtualDirectory(String virtualPath, String physicalPath): base(virtualPath)
        {
            DirectoryInfo info = new DirectoryInfo(physicalPath);

            String virtualPrefix = virtualPath.Substring(0, virtualPath.LastIndexOf("/") + 1);

			mDirectories = info.EnumerateDirectories().Select(childDirectory => 
				new VirtualDirectory(virtualPrefix + childDirectory.Name, info.FullName));

			mFiles = info.EnumerateFiles().Select(childFile =>
				new VirtualFile(virtualPrefix + childFile.Name, info.FullName));

			mChildren = mDirectories.Cast<VirtualFileBase>().Concat(mFiles.Cast<VirtualFileBase>());
        }

        /// <summary>
        /// Gets a list of the files and subdirectories contained in this virtual directory.
        /// </summary>
        /// <returns>An object implementing the <see cref="T:System.Collections.IEnumerable" /> interface containing <see cref="T:System.Web.Hosting.VirtualFile" /> and <see cref="T:System.Web.Hosting.VirtualDirectory" /> objects.</returns>
        public override IEnumerable Children
        {
            get
            {
                return mChildren;
            }
        }

        /// <summary>
        /// Gets a list of all the subdirectories contained in this directory.
        /// </summary>
        /// <returns>An object implementing the <see cref="T:System.Collections.IEnumerable" /> interface containing <see cref="T:System.Web.Hosting.VirtualDirectory" /> objects.</returns>
        public override IEnumerable Directories
        {
            get
            {
                return mDirectories;
            }
        }

        /// <summary>
        /// Gets a list of all files contained in this directory.
        /// </summary>
        /// <returns>An object implementing the <see cref="T:System.Collections.IEnumerable" /> interface containing <see cref="T:System.Web.Hosting.VirtualFile" /> objects.</returns>
        public override IEnumerable Files
        {
            get
            {
                return mFiles;
            }
        }
    }
}
