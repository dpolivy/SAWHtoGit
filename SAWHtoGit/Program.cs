/*
 * Copyright (c) 2013 Dan Polivy
 *
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the 
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS 
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAWHSDKLib;

namespace SAWHtoGit
{
	class Program
	{
		// ACTION: Set this to your desired working directory
		public static String WorkingDir = @"c:\temp\";

		static void Main(string[] args)
		{
			SAWHClient client = new SAWHClient();

			// Step 1: Log in to server
			// ACTION: Update the parameters with appropriate values based on your SAWH account (organization ID, username, password, and repo name)
			client.ConnectToServer(1 /* org id */, "User", "Password", "RepoName");

			Dictionary<string, string> movedFiles = new Dictionary<string, string>()
			{
				// ACTION: If you have any directories which were 'MOVED' within SourceAnywhere, you can map them here so the history will import in the new location
				// { "$/<source location>/", "$/<destination location>/" }
			};

			IDictionary<long, Changeset> changes;
			
			// Step 2: Get all of the changes that comprise the project history, and group them into logical "changesets"
			// ACTION: Set the first parameter to the project you'd like to export
			changes = client.GetProjectHistory("$/", movedFiles);
		
			// ACTION: Set this parameter to your desired output filename
			GitExporter exporter = new GitExporter(@"c:\temp\sawh_export");
			long? lastId = null;

			// Step 3: Iterate through all of the history objects
			foreach (Changeset change in changes.Values)
			{
				// Fetch the files associated with the changeset
				client.GetChangeset(WorkingDir, change);

				// Write the changeset out to the export file
				exporter.CommitChangeset(change, lastId);
				lastId = change.Id;
			}

			// Step 4: Done.
			exporter.FinishAndClose(lastId);			
		}
	}
}
