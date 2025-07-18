#define EVENT_MODE
#define DEBUG_MODE
using Kit2.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
namespace Kit2
{
	public static class Platform
	{
		private static string DataPath
		{
			get
			{
				return KxPath.Fix(Application.dataPath);
			}
		}

		public struct Response
		{
			public bool isError;
			public string msg;
			public Response(string msg, bool isErr)
			{
				this.msg = msg;
				this.isError = isErr;
			}

			public override string ToString()
			{
				if (this.isError)
					return $"[ERR]{msg}";
				return msg;
			}
		}

		public struct Feedback
		{
			public string command;
			public Response[] responses;

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.Append('>');
				sb.AppendLine(command);
				foreach (var r in responses)
				{
					sb.AppendLine(r.ToString());
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// Send <see cref="CommandLineTask"/> and listen on the <see cref="Feedback"/>
		/// </summary>
		private class CommandLineTask : MyTask
		{
			public enum eState
			{
				Idle,
				Run,
				End,
			}
			public eState state { get; private set; } = eState.Idle;
			private Process process;

			private List<Response> result;

			public readonly string command;
			private System.Action<Feedback> callback;
			public CommandLineTask(string shell, string args = "", System.Action<Feedback> completed = null)
			{
				this.command = $"{shell} {args}";
				this.callback = completed;
				var p = new Process()
				{
					EnableRaisingEvents = true,
					StartInfo = new ProcessStartInfo()
					{
						FileName = shell,
						Arguments = args,
						WindowStyle = ProcessWindowStyle.Hidden,
						CreateNoWindow = true,
						UseShellExecute = false,
						// WorkingDirectory = System.Environment.CurrentDirectory, //ProjectPath,
						WorkingDirectory = DataPath,
						// ErrorDialog = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						// RedirectStandardInput = true,
					}
				};

				this.result = new List<Response>();
				this.process = p;
#if EVENT_MODE
				this.process.OutputDataReceived += OutputDataReceived;
				this.process.ErrorDataReceived += ErrorDataReceived;
#endif
			}
#if EVENT_MODE
			private void OutputDataReceived(object sender, DataReceivedEventArgs e)
			{
				if (e.Data == null)
					return;
				result.Add(new Response(e.Data, false));
			}

			private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
			{
				if (e.Data == null)
					return;
				result.Add(new Response(e.Data, true));
			}
#endif
			protected override bool InternalExecute()
			{
				if (isDisposed)
					return false;
				if (state == eState.Idle)
				{
					state = eState.Run;
#if EVENT_MODE
					this.process.Start();
					this.process.BeginOutputReadLine();
					this.process.BeginErrorReadLine();
#else
                    this.process.Start();
                    using (var stdout = process.StandardOutput)
                    {
                        string line;
                        while ((line = stdout.ReadLine()) != null)
                            result.Add(new Response(line, false));
                    }
                    using (var stderr = process.StandardError)
                    {
                        string line;
                        while ((line = stderr.ReadLine()) != null)
                            result.Add(new Response(line, true));
                    }
#endif
				}
				if (state == eState.Run)
				{
					if (this.process.HasExited)
					{
						state = eState.End;
					}
				}
				if (state == eState.End)
				{
				}
				return state != eState.End;
			}

			#region Dispose
			protected override void OnDisposing()
			{
				base.OnDisposing();
				if (this.callback != null)
				{
					var rst = new Feedback
					{
						command = this.command,
						responses = result.ToArray(),
					};
					this.callback?.Invoke(rst);
				}
				state = eState.End;
				if (process != null)
				{
					try
					{
#if EVENT_MODE
						this.process.OutputDataReceived -= OutputDataReceived;
						this.process.ErrorDataReceived -= ErrorDataReceived;
#endif
						if (!process.HasExited)
						{
							process.Dispose();
							process.CloseMainWindow();
							process.Kill();
							UnityEngine.Debug.Log("Process suspend.");
						}
					}
					catch (System.Exception ex)
					{
						UnityEngine.Debug.LogError("Fail to suspend OS process " + ex.Message);
					}
					finally
					{
						process = null;
						callback = null;
						result.Clear();
					}
				}
			}
			#endregion Dispose
		}
		
		/// <summary>Run OS Command</summary>
		/// <param name="shell"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <see cref="http://forum.unity3d.com/threads/start-a-external-package.process.17488/"/>
		/// <seealso cref="http://ss64.com/nt/findstr.html"/>
		// [System.Diagnostics.Conditional("UNITY_EDITOR")]
		public static void CommandLine(string shell, string args = "", System.Action<Feedback> completed = null)
		{
#if DEBUG_MODE
			var t = new CommandLineTask(shell, args, (b) => { InternalDebugFeedBack(b); completed?.Invoke(b); });
#else
            var t = new CommandLineTask(shell, args, completed);
#endif

			if (Application.isPlaying)
			{
				MyTaskHandler.Add(t);
			}
			else
			{
#if UNITY_EDITOR
				// Editor mode.
				MyEditorTaskHandler.Add(t);
#endif
			}
		}

		private static void InternalDebugFeedBack(Feedback feedBack)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine(feedBack.command);
			foreach (var line in feedBack.responses)
			{
				if (line.isError)
					sb.Append("Err:").AppendLine(line.msg);
				else
					sb.AppendLine(line.msg);
			}
			UnityEngine.Debug.Log(sb.ToString());
		}
	}
}