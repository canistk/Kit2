using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

namespace Kit2
{
	public static class KxPath
	{
		private const System.StringComparison IGNORE = System.StringComparison.OrdinalIgnoreCase;
		private static string FixPath(this string path)
		{
			// since Addressable only support '/', style.
			return path
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			// Win32 style. '\\' in explorer
			.Replace('/', '\\');
#else
			// Addressable, Android, IOS, WebGL
			.Replace('\\', '/');
#endif
		}

		public static string Fix(string path) => path.FixPath();

		public static string Combine(string dir, string name, string extension)
		{
			return Path.Combine(dir, $"{name}.{extension}").FixPath(); ;
		}

		public static string Combine(params string[] args) => string.Join('/', args).FixPath();

		public static string GetDirectoryName(string path)
		{
			return Path.GetDirectoryName(path).FixPath();
		}

		public static string GetFileNameWithoutExtension(string path)
		{
			return Path.GetFileNameWithoutExtension(path);
		}

		public static string GetExtension(string path)
		{
			return Path.GetExtension(path).Trim();
		}

		public static bool HasExtension(string path)
		{
			return Path.HasExtension(path);
		}

		public static string ChangeExtension(string path, string extension)
		{
			return Path.ChangeExtension(path, extension);
		}

		public static bool Exists(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("Path is null or empty.");
				return false;
			}

			// TODO: android check if path is a valid path.
			if (path.Contains("://"))
			{
				// assume path are getting from android are contain "://" string.
				// e.g. Application.streamingAssetsPath
				var request = UnityWebRequest.Get(path);
				var operation = request.SendWebRequest();
				// wait for the request to complete
				while (!operation.isDone)
				{
					// yield return null; // if you are in a coroutine, you can yield here.
					if (request.result == UnityWebRequest.Result.ConnectionError ||
						request.result == UnityWebRequest.Result.ProtocolError)
					{
						Debug.LogError($"Error checking file existence: {request.error}");
						return false;
					}
				}

				return UnityWebRequest.Get(path).isDone;
			}
			else
			{
				path = path.FixPath();
				return File.Exists(path);
			}
		}

		private static Regex s_ExtensionRule = new Regex(@"^.*\.([a-zA-Z0-9]{1,5})$");
		public static bool IsExtension(string path, bool ignoreCase, params string[] extensions)
		{
			if (extensions.Length == 0)
			{
				Debug.LogWarning("invalid extensions input. cannot be 0");
				return false;
			}

			if (!HasExtension(path))
			{
				Debug.LogError($"Path didn't contain file extension. {path}");
				return false;
			}
				
			var ext = GetExtension(path);

			foreach (var extension in extensions)
			{
				// common input error.
				if (!s_ExtensionRule.IsMatch(extension))
				{
					Debug.LogWarning($"{extension} is invalid format. are you missing a '.' in front of file extension?");
					continue;
				}

				var tag = extension.StartsWith('*') ? extension.Substring(1) : extension;

				if (ignoreCase)
				{
					if (tag.Equals(ext, IGNORE))
						return true;
				}
				else
				{
					if (tag.Equals(ext))
						return true;
				}
			}
			return false;
		}
	}

	public static class KxDirectory
	{
		public static void EnsureExists(string pathName)
		{
			if (Exists(pathName)) return;
			CreateDirectory(pathName);
		}

		public static void CreateDirectory(string pathName)
		{
			if (Exists(pathName)) return;
			try
			{
				Directory.CreateDirectory(pathName);
			}
			catch (System.Exception e)
			{
				Debug.LogErrorFormat("Create directory failed : [{0}]\n{1}", pathName, e);
			}
		}

		public static bool Exists(string pathName)
		{
			return Directory.Exists(pathName);
		}

		public static void CopyFile(string srcFilename, string dstFilname, bool overwrite, bool createDir)
		{
			if (!File.Exists(srcFilename))
			{
				Debug.LogErrorFormat("Copying non existing file : [{0}]", srcFilename);
				return;
			}

			if (createDir)
			{
				var dst = Path.GetDirectoryName(dstFilname);
				EnsureExists(dst);
			}

			try
			{
				File.Copy(srcFilename, dstFilname, overwrite);
			}
			catch (System.Exception e)
			{
				Debug.Log(e);
			}
		}

		public static void CopyDir(string srcDir, string dstDir, bool overwrite)
		{
			var srcSubDirs = Directory.GetDirectories(srcDir);
			var srcFiles = Directory.GetFiles(srcDir);

			EnsureExists(dstDir);

			foreach (var sd in srcSubDirs)
			{
				var fname = Path.GetFileName(sd);
				var dd = Path.Combine(dstDir, fname);

				CopyDir(sd, dd, overwrite);
			}

			foreach (var f in srcFiles)
			{
				var dstFilename = Path.Combine(dstDir, Path.GetFileName(f));
				CopyFile(f, dstFilename, overwrite, false);
			}
		}

		public static string[] GetFiles(string path)
		{
			return Directory.GetFiles(path);
		}

		public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			return Directory.GetFiles(path, searchPattern, searchOption);
		}

		public static IEnumerable<string> EnumerateDirectories(string path)
		{
			return Directory.EnumerateDirectories(path);
		}
	}

	public static class KxFile
	{
		public static bool Exists(string path)
		{
#if UNITY_ANDROID
            // throw new System.NotImplementedException();
            Debug.LogError("Not support file.exists on platform.");
            return false;
#else
			return System.IO.File.Exists(path);
#endif
		}

		private static string GetDir(string relativePath)
		{
			if (relativePath == null || relativePath.Length == 0)
				return UnityEngine.Application.streamingAssetsPath;
			return Path.Combine(UnityEngine.Application.streamingAssetsPath, relativePath);
		}

		public static void WriteSA(string relativePath, string filename, string ext, string content, bool backup = true)
		{
			string dir = GetDir(relativePath);
			string file = $"{filename}.{ext}";
			string path = Path.Combine(dir, file);
			Write(path, content, backup);
		}

		public static void Write(string path, string content, bool backup = true)
		{
			const string EXT = ".bak";
			string dir = Path.GetDirectoryName(path);
			KxDirectory.EnsureExists(dir);

			if (File.Exists(path))
			{
				if (backup)
				{
					// Move to backup file, when path exist.
					var backupPath = KxPath.ChangeExtension(path, EXT);
					Move(path, backupPath);
				}
				else
				{
					File.Delete(path);
				}
			}

			File.WriteAllText(path, content);
		}

		public static void ReadBytes(string fullPath, System.Action<byte[]> callback)
		{
			// assume path are getting from android are contain "://" string.
			// e.g. Application.streamingAssetsPath
			if (fullPath.Contains("://"))
			{
				var www = UnityWebRequest.Get(fullPath);
				var oper = www.SendWebRequest();
				oper.completed += _OnWebReceived;

				void _OnWebReceived(AsyncOperation oper)
				{
					if (www.result == UnityWebRequest.Result.Success)
					{
						var content = www.downloadHandler.data;
						callback?.Invoke(content);
					}
					else
					{
						Debug.LogError("Error loading file: " + www.error);
					}
				}
			}
			else
			{
				if (!File.Exists(fullPath))
				{
					throw new System.IO.FileNotFoundException(fullPath);
				}

				var content = File.ReadAllBytes(fullPath);
				callback?.Invoke(content);
			}
		}

		public static void Read(string fullPath, System.Action<string> callback, System.Action<System.Exception> fail = null)
		{
			// assume path are getting from android are contain "://" string.
			// e.g. Application.streamingAssetsPath
			try
			{
				if (fullPath.Contains("://"))
				{
					var www = UnityWebRequest.Get(fullPath);
					var oper = www.SendWebRequest();
					oper.completed += _OnWebReceived;

					void _OnWebReceived(AsyncOperation oper)
					{
						if (www.result == UnityWebRequest.Result.Success)
						{
							string content = www.downloadHandler.text;
							callback?.Invoke(content);
						}
						else
						{
							Debug.LogError("Error loading file: " + www.error);
						}
					}
				}
				else
				{
					if (!File.Exists(fullPath))
					{
						throw new System.IO.FileNotFoundException(fullPath);
					}

					var content = File.ReadAllText(fullPath);
					callback?.Invoke(content);
				}
			}
			catch (System.IO.IOException ex)
			{
				if (fail == null)
					throw ex;
				else
					fail.TryCatchDispatchEventError(o => o?.Invoke(ex));
			}
		}

		public static void ReadSA(string relativePath, string nameTag, string ext, System.Action<string> callback)
		{
			string dir = GetDir(relativePath);
			string file = $"{nameTag}.{ext}";
			string path = Path.Combine(dir, file);//.Replace("\\","/");
			Read(path, callback, Debug.LogError);
		}

		public static void ReadSA<T>(string relativePath, string ext, System.Action<T> callback)
		{
			ReadSA(relativePath, typeof(T).Name, ext, (c) => callback?.Invoke(JsonConvert.DeserializeObject<T>(c)));
		}

		public static void ReadSA<T>(string relativePath, string ext, System.Action<T[]> callback)
		{
			ReadSA(relativePath, typeof(T).Name, ext, (c) => callback?.Invoke(JsonConvert.DeserializeObject<T[]>(c)));
		}

		/*
		public static void ReadSAFromZip<T>(string zipFilename, string filename, string password, System.Action<T> callback, System.Action<System.Exception> onFail = null)
		{
			try
			{
				ReadSAFromZip(zipFilename, filename, password, (txt) =>
				{
					var raw = JsonConvert.DeserializeObject<T>(txt);
					callback?.Invoke(raw);
				}, (ex) => throw ex);
			}
			catch (System.Exception ex)
			{
				if (onFail == null)
					// ex.DeepLogInvocationException($"ReadSAFromZip {filename} fail.");
					throw ex;
				else
					onFail.TryCatchDispatchEventError(o => o?.Invoke(ex));
			}
		}

		public static void ReadSAFromZip(string zipFilename, string filename, string password, System.Action<string> callback, System.Action<System.Exception> onFail = null)
		{
			try
			{
				bool found = false;
				using (ZipFile zipFile = new ZipFile(zipFilename))
				{
					zipFile.Password = password;

					foreach (ZipEntry entry in zipFile)
					{
						if (entry.Name != filename)
						{
							continue;
						}

						found = true;
						using (var zipReader = zipFile.GetInputStream(entry))
						using (var streamReader = new System.IO.StreamReader(zipReader))
						{
							var allText = streamReader.ReadToEnd();
							callback?.Invoke(allText);
						}
					}

					if (!found)
						throw new System.Exception($"{filename} not found.");
				}
			}
			catch (System.Exception ex)
			{
				if (onFail == null)
					// ex.DeepLogInvocationException($"ReadSAFromZip {filename} fail.");
					throw ex;
				else
					onFail.TryCatchDispatchEventError(o => o?.Invoke(ex));
			}
		}
		//*/
		public static void Move(string sourcePath, string destPath)
		{
			if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath))
			{
				Debug.LogError("Source or destination path is null or empty.");
				return;
			}
			if (!File.Exists(sourcePath))
			{
				Debug.LogError($"Source file does not exist: {sourcePath}");
				return;
			}
			if (File.Exists(destPath))
			{
				// Debug.LogWarning($"Destination file already exists and will be overwritten: {destPath}");
				try
				{
					File.Delete(destPath);
				}
				catch (System.Exception e)
				{
					Debug.LogError($"Failed to delete existing file at destination: {e.Message}");
					return;
				}
			}
			File.Move(sourcePath, destPath);
		}
		public static bool Delete(string path)
		{

			if (File.Exists(path))
			{
				File.Delete(path);
				return true;
			}
			else
			{
				return false;
			}
		}

		public static void WriteWithBackupByDate(string path, string content)
		{
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogError("Path is null or empty.");
				return;
			}

			string dir = Path.GetDirectoryName(path);
			KxDirectory.EnsureExists(dir);

			if (KxFile.Exists(path))
			{
				var fName = KxPath.GetFileNameWithoutExtension(path);
				var date = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
				var backupPath = KxPath.Combine(dir, $"{fName}_{date}.bak");
				KxFile.Move(path, backupPath);
			}
			File.WriteAllText(path, content);
		}

	}
}