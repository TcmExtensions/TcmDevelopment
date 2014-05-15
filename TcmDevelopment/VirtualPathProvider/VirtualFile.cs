#region Header
////////////////////////////////////////////////////////////////////////////////////
//
//	File Description: VirtualFile
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
using System.IO;

namespace TcmDevelopment.VirtualPathProvider
{
    /// <summary>
    /// <see cref="VirtualFile" /> exposes a "file" object in our virtual filesystem
    /// </summary>
    internal class VirtualFile : System.Web.Hosting.VirtualFile
    {
        private String mPhysicalPath;

		/// <summary>
		/// Initializes a new instance of the <see cref="VirtualFile"/> class.
		/// </summary>
		/// <param name="virtualPath">The virtual path.</param>
		/// <param name="physicalPath">The physical path.</param>
        public VirtualFile(String virtualPath, String physicalPath): base(virtualPath)
        {
            mPhysicalPath = physicalPath;
        }

		/// <summary>
		/// When overridden in a derived class, returns a read-only stream to the virtual resource.
		/// </summary>
		/// <returns>
		/// A read-only stream to the virtual file.
		/// </returns>
        public override Stream Open()
        {
            return new FileStream(mPhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
    }
}
