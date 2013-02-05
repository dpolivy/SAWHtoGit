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
using System.IO;

namespace SAWHtoGit
{
	// Helper class to create a git fast-import script
	class GitExporter
	{
		FileStream ExportLog;
		System.Text.Encoding Encoding = new System.Text.ASCIIEncoding();
		String Filename;
		int LastFileSegment = 1;

		public GitExporter(string filename)
		{
			// Export to a file
			Filename = filename;
			ExportLog = File.Open(filename + "_" + LastFileSegment++, FileMode.Create);
			WriteRawString("reset refs/heads/master\n");
		}

		public void FinishAndClose(long? finalMark)
		{
			WriteRawString("reset refs/heads/master\n");
			if (finalMark != null) WriteRawString(String.Format("from :{0}\n", finalMark));
			WriteRawString("done\n\n");
			ExportLog.Close();
		}

		public string GetUserName(string user)
		{
			string mappedUser;

			switch (user)
			{
				default: mappedUser = "User <user@domain.com>"; break;
			}

			return mappedUser;
		}

		// Write a raw string to the output
		void WriteRawString(string data)
		{
			byte[] rawData = Encoding.GetBytes(data);
			ExportLog.Write(rawData, 0, rawData.Length);
		}

		// Write a git data blob
		public void WriteData(byte[] data)
		{
			WriteRawString(String.Format("data {0}\n", data.Length));
			ExportLog.Write(data, 0, data.Length);
			WriteRawString("\n");
		}

		// Write a git data blob from a string
		public void WriteData(string data)
		{
			byte[] rawData = Encoding.GetBytes(data);
			WriteData(rawData);
		}

		public void WriteInlineFile(string filename, string code = "M", string mode = "100644")
		{
			using (FileStream fsSource = new FileStream(Program.WorkingDir + '\\' + filename.Replace('/','\\'), FileMode.Open, FileAccess.Read))
			{
				// Read the source file into a byte array. 
				byte[] bytes = new byte[fsSource.Length];
				int numBytesToRead = (int)fsSource.Length;
				int numBytesRead = 0;
				while (numBytesToRead > 0)
				{
					// Read may return anything from 0 to numBytesToRead. 
					int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

					// Break when the end of the file is reached. 
					if (n == 0)
						break;

					numBytesRead += n;
					numBytesToRead -= n;
				}
				numBytesToRead = bytes.Length;

				WriteRawString(String.Format("{0} {1} inline {2}\n", code, mode, filename));
				WriteData(bytes);
			}

		}

		public void WriteCommit(long mark, long? lastMark, string author, DateTime date, string comment)
		{
			WriteRawString(String.Format("progress Commiting changeset {0}\n\n", mark));
			WriteRawString(String.Format("commit refs/heads/master\n"));
			WriteRawString(String.Format("mark :{0}\n", mark));
			WriteRawString(String.Format("committer {0} {1} -0800\n", GetUserName(author), ConvertDateTimeToEpoch(date)));
			WriteData(comment);
			if (lastMark != null) WriteRawString(String.Format("from :{0}\n", lastMark)); // Uncomment this if you're building multiple/diff import files //else WriteRawString("from refs/heads/master^0\n");
		}

		public void CommitChangeset(Changeset change, long? prevChangeId)
		{
			// First, write the commit header
			WriteCommit(change.Id, prevChangeId, change.Author, change.CommitDate, change.Comment);
			foreach (SourceFile file in change.Files)
			{
				WriteInlineFile(file.SourceName.Substring(2));
			}
			WriteRawString("\n");
			ExportLog.Flush();
			/*
			// Uncomment this to keep the files below a certain size
			if (ExportLog.Position > 200000000)
			{
				ExportLog.Close();
				ExportLog = File.Open(Filename + "_" + LastFileSegment++, FileMode.Create);
			}
			*/
		}

		public static double ConvertDateTimeToEpoch(DateTime time)
		{
			DateTime epoch = new DateTime(1970, 1, 1);

			return time.ToUniversalTime().Subtract(epoch).TotalSeconds;
		}
	}
}
