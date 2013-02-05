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
	// Helper class to interface with the SAWH API
	class SAWHClient
	{
		SAWHAppObject sawObject;

		// Login to the server and connect to the specified repository
		public bool ConnectToServer(int orgId, string user, string password, string repo) {
			sawObject = new SAWHAppObject();

			long ResultValue;
			Enum_EncryptType EncryptType;
			SAWHGlobalMessageSet GlobalMessageSet;
			string PlanName;
			bool Cancelled;
			string ResultDescription;

			sawObject.SetConnectionType(Enum_ConnectionType.Enum_BroadBandConnection);

			ResultValue = sawObject.ConnectToServer(orgId, false, true, "", 0, Enum_ProxyType.Enum_NOPROXY, "", 0, "", "", out EncryptType, out GlobalMessageSet, out PlanName, out Cancelled, out ResultDescription);
			Console.WriteLine("Result: {0}, {1}", ResultValue, ResultDescription);
			if (ResultValue == 0)
			{
				bool MustChangePassword;
				int DaysOfExpiration;
				bool Cancelled1;
				string ResultDescription1;

				sawObject.Login(user, password, repo, out MustChangePassword, out DaysOfExpiration, out Cancelled1, out ResultDescription1);
				Console.WriteLine("Login: {0}", ResultDescription1);

				return true;
			}

			return false;
		}

		// Given a project, return all of the associated changesets
		public IDictionary<long, Changeset> GetProjectHistory(string project, IDictionary<string, string> movedFiles)
		{
			long ResultValue;
			SAWHProjectHistoryParam HistoryParams;
			SAWHHistorySet HistorySet;
			bool Cancelled;
			string ResultDescription;
			SAWHHistoryOrderBy HistoryOrderBy;

			SortedDictionary<long, Changeset> History = new SortedDictionary<long, Changeset>();

			HistoryOrderBy = new SAWHHistoryOrderBy();
			HistoryOrderBy.HistorySortBy = Enum_HistorySortBy.Enum_HistorySortByDate;
			HistoryParams = new SAWHProjectHistoryParam();
			HistoryParams.IsRecursive = true;
			HistoryParams.AddOrder(HistoryOrderBy);

			// ACTION: You can further tailor the history fetched by adding additional filters, such as the date one below. Note, dates are in UTC.
			//HistoryParams.DateFrom = new DateTime(2013, 1, 14).ToUniversalTime();

			Console.WriteLine("Fetching history for project {0}", project);

			ResultValue = sawObject.GetProjectHistory(project, HistoryParams, out HistorySet, out Cancelled, out ResultDescription);
			if (ResultValue == 0)
			{
				long Count = HistorySet.Count;

				foreach (ISAWHHistory HistoryItem in HistorySet) {
					/*
					// Debugging info on the fetched history item
					Console.WriteLine("History Item: {0}\t{1}\t{2}\t{3}", 
						HistoryItem.ChangeSetIdLow | (HistoryItem.ChangeSetIdHigh << 32),
						HistoryItem.HistoryIdLow | (HistoryItem.HistoryIdHigh <<32),
						HistoryItem.ActionType.ToString(), HistoryItem.ActionDescription);
					Console.WriteLine("\tby: {0}: {1}", HistoryItem.UserName, HistoryItem.Comment);
					Console.WriteLine("\tItem: v{0}\t{1}\t{2}", HistoryItem.VersionNumberLow | (HistoryItem.VersionNumberHigh << 32), HistoryItem.ItemIdLow | (HistoryItem.ItemIdHigh << 32), HistoryItem.ItemName);
					Console.WriteLine("\ton: {0}\t{1}\t{2}", HistoryItem.ActionDateTime, HistoryItem.CheckinDateTime, HistoryItem.ModificationDateTime);
					Console.WriteLine("\tMerge: {0}\tProject: {1}", HistoryItem.IsMergable, HistoryItem.IsProject);
					*/
					 
					if (!HistoryItem.IsProject)
					{
						Changeset changes;
						long historyId = HistoryItem.HistoryIdLow | (HistoryItem.HistoryIdHigh << 32);
						long key = HistoryItem.CheckinDateTime.Ticks;
						string filename = HistoryItem.ItemName;

						// Establish the associated changeset
						if (!History.TryGetValue(key, out changes))
						{
							// Create new Changeset
							changes = new Changeset();
							changes.CommitDate = HistoryItem.CheckinDateTime;
							changes.Comment = HistoryItem.Comment;
							changes.Author = HistoryItem.UserName;
							changes.Id = historyId;
							changes.Files = new List<SourceFile>();

							History[key] = changes;
						}

						// For folders that have moved within source control, map them to the "new" path so SAW can find them
						foreach (KeyValuePair<string, string> mapping in movedFiles)
						{
							if (filename.StartsWith(mapping.Key))
							{
								filename = filename.Replace(mapping.Key, mapping.Value);
							}
						}

						// Add the current file to a changeset
						changes.Files.Add(new SourceFile(filename, HistoryItem.VersionNumberLow, HistoryItem.VersionNumberHigh, HistoryItem.IsMergable ? SourceFile.FileType.Text : SourceFile.FileType.Binary, HistoryItem.ItemIdLow, HistoryItem.ItemIdHigh));
					}
				}

				Console.WriteLine("Wrote {1} changesets containing {0} versions", Count, History.Count);

				/*
				// Debug code to enumerate all found changesets
				foreach (Changeset change in History.Values)
				{
					Console.WriteLine("Change: {0} by {1} on {2}: \"{3}\"", change.Id, change.Author, change.CommitDate, change.Comment);
					foreach (SourceFile file in change.Files)
					{
						Console.WriteLine("\tFile: {0} {1} {2}", file.VersionLow, file.ItemId, file.SourceName);
					}
				}
				*/
			}

			return History;
		}

		// Given a changeset, fetch all of the associated files
		public bool GetChangeset(string localpath, Changeset change)
		{
			long ResultValue;
			bool Cancelled;
			string ResultDescription;
			SAWHDiffMergeParam DiffMergeParam = new SAWHDiffMergeParam();

			Console.WriteLine("Fetching changeset {0} ({1}) by {2}: {3}", change.Id, change.CommitDate, change.Author, change.Comment);

			foreach (SourceFile file in change.Files.ToList())
			{
				ResultValue = sawObject.GetOldVersionFile(file.SourceName, file.VersionLow, file.VersionHigh, localpath + file.SourceName.Trim('$').Replace('/','\\'), 
					false, Enum_ModifiedFileHandling.Enum_ReplaceModifiedFile, Enum_EOL.Enum_EOLNative, Enum_CompareFileBy.Enum_CompareFileByChecksum,
					Enum_SetLocalFileTime.Enum_SetLocalFileTimeCheckIn, DiffMergeParam, out Cancelled, out ResultDescription);
				//Console.WriteLine("{0}: Fetched {1} --> {2}", ResultValue, file.SourceName, localpath + file.SourceName.Trim('$').Replace('/', '\\'));
				if (ResultValue != 0)
				{
					// If there was an error fetching the file, remove it from the list so we don't try to add it to git
					Console.WriteLine("ERROR: {0}\t {1}", ResultValue, ResultDescription);
					change.Files.Remove(file);
				}
			}

			return true;
		}
	}
}
