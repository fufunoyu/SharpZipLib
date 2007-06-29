// SharpZipLibrary samples
// Copyright (c) 2007, AlphaSierraPapa
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this list
//   of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials
//   provided with the distribution.
//
// - Neither the name of the SharpDevelop team nor the names of its contributors may be used to
//   endorse or promote products derived from this software without specific prior written
//   permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace Samples.FastZipDemo
{
	class MainClass
	{
		enum Operation
		{
			Unknown,
			Create,
			Extract,
			List,
			Error
		};

		static void ListZipFile(string fileName, string fileFilter, string directoryFilter)
		{
			using (ZipFile zipFile = new ZipFile(fileName)) {
				PathFilter localFileFilter = new PathFilter(fileFilter);
				PathFilter localDirFilter = new PathFilter(directoryFilter);
				
				if ( zipFile.Count == 0 ) {
					Console.WriteLine("No entries to list");
				}
				else {
					for ( int i = 0 ; i < zipFile.Count; ++i)
					{
						ZipEntry e = zipFile[i];
						if ( e.IsFile ) {
							string path = Path.GetDirectoryName(e.Name);
							if ( localDirFilter.IsMatch(path) ) {
								if ( localFileFilter.IsMatch(Path.GetFileName(e.Name)) ) {
									Console.WriteLine(e.Name);
								}
							}
						}
						else if ( e.IsDirectory ) {
							if ( localDirFilter.IsMatch(e.Name) ) {
								Console.WriteLine(e.Name);
							}
						}
						else {
							Console.WriteLine(e.Name);
						}
					}
				}
			}
		}
		
		void ListFile(object sender, ScanEventArgs e)
		{
			Console.WriteLine("{0}", e.Name);
		}
		
		void ListDir(object Sender, DirectoryEventArgs e)
		{
			if ( !e.HasMatchingFiles ) {
				Console.WriteLine("Dir:{0}", e.Name);
			}
		}

		void ListFileSystem(string directory, bool recurse, string fileFilter, string directoryFilter)
		{
			FileSystemScanner scanner = new FileSystemScanner(fileFilter, directoryFilter);
			scanner.ProcessDirectory += new ProcessDirectoryDelegate(ListDir);
			scanner.ProcessFile += new ProcessFileDelegate(ListFile);
			scanner.Scan(directory, recurse);
		}
		
		void ProcessFile(object sender, ScanEventArgs e)
		{
			Console.WriteLine(e.Name);
		}
		
		void ProcessDirectory(object sender, DirectoryEventArgs e)
		{
			if ( !e.HasMatchingFiles ) {
				Console.WriteLine(e.Name);
			}
		}

		bool ConfirmOverwrite(string file)
		{
			Console.WriteLine("Overwrite file {0} Y/N", file);
			string yesNo = Console.ReadLine();
			return string.Compare(yesNo.Trim(), "y", true) == 0;
		}
		
		void Run(string[] args)
		{
			bool recurse = false;
			string arg1 = null;
			string arg2 = null;
			string fileFilter = null;
			string dirFilter = null;
			bool verbose = false;
			bool restoreDates = false;
			bool restoreAttributes = false;

			bool createEmptyDirs = false;
			FastZip.Overwrite overwrite = FastZip.Overwrite.Always;
			FastZip.ConfirmOverwriteDelegate confirmOverwrite = null;
			
			Operation op = Operation.Unknown;
			int argCount = 0;
			
			for ( int i = 0; i < args.Length; ++i ) {
				if ( args[i][0] == '-' ) {
					string option = args[i].Substring(1).ToLower();
					string optArg = "";
	
					int parameterIndex = option.IndexOf('=');
	
					if (parameterIndex >= 0)
					{
						if (parameterIndex < option.Length - 1) {
							optArg = option.Substring(parameterIndex + 1);
						}
						option = option.Substring(0, parameterIndex);
					}
					
					switch ( option ) {
						case "e":
						case "empty":
							createEmptyDirs = true;
							break;
							
						case "x":
						case "extract":
							if ( op == Operation.Unknown ) {
								op = Operation.Extract;
							}
							else {
								Console.WriteLine("Only one operation at a time is permitted");
								op = Operation.Error;
							}
							break;
							
						case "c":
						case "create":
							if ( op == Operation.Unknown ) {
								op = Operation.Create;
							}
							else {
								Console.WriteLine("Only one operation at a time is permitted");
								op = Operation.Error;
							}
							break;

						case "l":
						case "list":
							if ( op == Operation.Unknown ) {
								op = Operation.List;
							}
							else {
								Console.WriteLine("Only one operation at a time is permitted");
								op = Operation.Error;
							}
							break;

							
						case "r":
						case "recurse":
							recurse = true;
							break;
							
						case "v":
						case "verbose":
							verbose = true;
							break;
							
						case "file":
							if ( NameFilter.IsValidFilterExpression(optArg) ) {
								fileFilter = optArg;
							}
							else {
								Console.WriteLine("File filter expression contains an invalid regular expression");
								op = Operation.Error;
							}
							break;
							
						case "dir":
							if ( NameFilter.IsValidFilterExpression(optArg) ) {
								dirFilter = optArg;
							}
							else {
								Console.WriteLine("Path filter expression contains an invalid regular expression");
								op = Operation.Error;
							}
							break;
							
						case "o":
						case "overwrite":
							switch ( optArg )
							{
								case "always":
									overwrite = FastZip.Overwrite.Always;
									confirmOverwrite = null;
									break;
									
								case "never":
									overwrite = FastZip.Overwrite.Never;
									confirmOverwrite = null;
									break;
									
								case "prompt":
									overwrite = FastZip.Overwrite.Prompt;
									confirmOverwrite = new FastZip.ConfirmOverwriteDelegate(ConfirmOverwrite);
									break;
									
								default:
									Console.WriteLine("Invalid overwrite option");
									op = Operation.Error;
									break;
							}
							break;
							
						case "oa":
							restoreAttributes = true;
							break;
							
						case "od":
							restoreDates = true;
							break;
							
						default:
							Console.WriteLine("Unknown option {0}", args[i]);
							op = Operation.Error;
							break;
					}
				}
				else if ( arg1 == null ) {
					arg1 = args[i];
					++argCount;
				}
				else if ( arg2 == null ) {
					arg2 = args[i];
					++argCount;
				}
			}

			FastZipEvents events = null;
			
			if ( verbose ) {
				events = new FastZipEvents();
				events.ProcessDirectory = new ProcessDirectoryDelegate(ProcessDirectory);
				events.ProcessFile = new ProcessFileDelegate(ProcessFile);
			}
			
			FastZip sz = new FastZip(events);
			sz.CreateEmptyDirectories = createEmptyDirs;
			sz.RestoreAttributesOnExtract = restoreAttributes;
			sz.RestoreDateTimeOnExtract = restoreDates;
			
			switch ( op ) {
				case Operation.Create:
					if ( argCount == 2 ) {
						Console.WriteLine("Creating Zip");

						sz.CreateZip(arg1, arg2, recurse, fileFilter, dirFilter);
					}
					else
						Console.WriteLine("Invalid arguments");
					break;
					
				case Operation.Extract:
					if ( argCount == 2 ) {
						Console.WriteLine("Extracting Zip");
						sz.ExtractZip(arg1, arg2, overwrite, confirmOverwrite, fileFilter, dirFilter, recurse);
					}
					else
						Console.WriteLine("zipfile and target directory not specified");
					break;
					
				case Operation.List:
					if ( File.Exists(arg1) ) {
						ListZipFile(arg1, fileFilter, dirFilter);
					}
					else if ( Directory.Exists(arg1) ) {
						ListFileSystem(arg1, recurse, fileFilter, dirFilter);
					}
					else {
						Console.WriteLine("No valid list file or directory");
					}
					break;
					
				case Operation.Unknown:
					Console.WriteLine(
					   "FastZip v0.4\n"
					+  "  Usage: FastZip {options} operation args\n"
					+  "Operation Options: (only one permitted)\n"
					+  "  -x zipfile targetdir : Extract files from Zip\n"
					+  "  -c zipfile sourcedir : Create zip file\n"
					+  "  -l zipfile|dir       : List elements\n"
					+  "\n"
					+  "Behavioural options:\n"
					+  "  -file={fileFilter}\n"
					+  "  -dir={dirFilter}\n"
					+  "  -e Process empty directories\n"
					+  "  -r Recurse directories\n"
					+  "  -v Verbose output\n"
					+  "  -oa Restore file attributes on extract\n"
					+  "  -ot Restore file date time on extract\n"
					+  "  -overwrite=prompt|always|never   : Overwrite on extract handling\n"
					);
					break;
				
				case Operation.Error:
					// Do nothing for now...
					break;
			}
		}
		
		/// <summary>
		/// Main entry point for FastZip sample.
		/// </summary>
		/// <param name="args">The arguments provided to this process.</param>
		public static void Main(string[] args)
		{
			MainClass main = new MainClass();
			main.Run(args);
		}
	}
}
