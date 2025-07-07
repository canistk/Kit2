using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
namespace Kit2
{
	public static class KxPath
	{
		public static string Combine(string dir, string name, string extension)
		{
			return Path.Combine(dir, $"{name}.{extension}");
		}

		public static string Combine(params string[] args) => string.Join('/', args).Replace('\\', '/');

		public static string GetDirectoryName(string path)
		{
			return Path.GetDirectoryName(path).Replace('\\', '/');
		}

		public static string GetFileNameWithoutExtension(string path)
		{
			return Path.GetFileNameWithoutExtension(path);
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
			const string ext = "_bak.bak";
			string dir = Path.GetDirectoryName(path);
			KxDirectory.EnsureExists(dir);

			if (File.Exists(path))
			{
				if (backup)
				{
					// Move to backup file, when path exist.
					string bak = Path.ChangeExtension(path, ext);
					if (File.Exists(bak))
						File.Delete(bak);
					// move current target folder in to backup.
					File.Move(path, bak);
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
	}
}