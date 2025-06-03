using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using UnityEngine.Profiling;

namespace Kit2
{
	public static class SystemExtend
    {
        #region Debug Functions
        /// <summary>Gets the methods of an object.</summary>
        /// <returns>A list of methods accessible from this object.</returns>
        /// <param name='obj'>The object to get the methods of.</param>
        /// <param name='includeInfo'>Whether or not to include each method's method info in the list.</param>
        public static string MethodsOfObject(this System.Object obj, bool includeInfo = false)
        {
            string methods = string.Empty;
            MethodInfo[] methodInfos = obj.GetType().GetMethods();
            for (int i = 0; i < methodInfos.Length; i++)
            {
                if (includeInfo)
                {
                    methods += methodInfos[i] + "\n";
                }
                else
                {
                    methods += methodInfos[i].Name + "\n";
                }
            }
            return (methods);
        }

        /// <summary>Gets the methods of a type.</summary>
        /// <returns>A list of methods accessible from this type.</returns>
        /// <param name='type'>The type to get the methods of.</param>
        /// <param name='includeInfo'>Whether or not to include each method's method info in the list.</param>
        public static string MethodsOfType(this Type type, bool includeInfo = false)
        {
            string methods = string.Empty;
            MethodInfo[] methodInfos = type.GetMethods();
            for (var i = 0; i < methodInfos.Length; i++)
            {
                if (includeInfo)
                {
                    methods += methodInfos[i] + "\n";
                }
                else
                {
                    methods += methodInfos[i].Name + "\n";
                }
            }
           return (methods);
        }

		/// <summary>Use reflection to invoke function by Name</summary>
		/// <param name="obj">This object</param>
		/// <param name="functionName">function name in string</param>
		/// <param name="bindingFlags"><see cref="BindingFlags"/></param>
		/// <param name="args">The values you wanted to pass, will trim out if destination params less than provider.</param>
		/// <returns></returns>
		public static bool InvokeMethod(this object obj, string functionName, BindingFlags bindingFlags, params object[] args)
		{
			Type type = obj.GetType();
			MethodInfo method = type.GetMethod(functionName, bindingFlags);
			if (method != null)
			{
				int length = method.GetParameters().Length;
				if (length > args.Length)
				{
					throw new ArgumentOutOfRangeException("Destination parameter(s) are required " + length + ", but system provided " + args.Length);
				}
				else
				{
					object[] trimArgs = new object[length];
					Array.Copy(args, trimArgs, length);
					method.Invoke(obj, trimArgs);
					return true;
				}
			}
			return false;
		}

        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        public static T Clone<T>(this UnityEngine.Object obj) where T : UnityEngine.Object
        {
            if (obj == null)
                throw new System.NullReferenceException();
            var type    = obj.GetType();
            T rst       = (T)Activator.CreateInstance(type);
            foreach (var property in type.GetProperties())
            {
                if (property.GetSetMethod() != null && property.GetGetMethod() != null)
                {
                    property.SetValue(rst, property.GetValue(obj, null), null);
                }
            }
            return rst;
        }
		#endregion Debug Functions

		#region Exception
		/// <summary>A event dispatcher helper to dispatch event with try catch block.</summary>
		/// <typeparam name="EVENT"></typeparam>
		/// <param name="evt"></param>
		/// <param name="singleDispatcher"></param>
		/// <param name="maxDepth"></param>
		/// <exception cref="System.Exception"></exception>
		public static void TryCatchDispatchEventError<EVENT>(this EVENT evt, System.Action<EVENT> singleDispatcher, int maxDepth = -1)
			where EVENT : System.MulticastDelegate
		{
			if (evt == null)
				return;
			if (singleDispatcher == null)
				throw new System.Exception("Must define how dispatcher handle the event.");

			var handles = evt.GetInvocationList();
			for (var i = 0; i < handles.Length; ++i)
			{
				try
				{
					var dispatcher = handles[i] as EVENT;
					if (dispatcher == null)
						continue;
					singleDispatcher?.Invoke(dispatcher);
				}
				catch (System.Exception ex)
				{
					ex.DeepLogInvocationException(evt.GetType().Name, handles[i], maxDepth);
				}
			}
		}

		/// <summary>A helper to log exception stack trace in a more readable way.</summary>
		/// <param name="ex"></param>
		/// <param name="eventDispatcherName"></param>
		/// <param name="delegatehandler"></param>
		/// <param name="maxDepth"></param>
		public static void DeepLogInvocationException(this Exception ex, string eventDispatcherName, Delegate delegatehandler, int maxDepth = -1)
		{
			ex.DeepLogInvocationException($"{eventDispatcherName} > {(delegatehandler?.Target ?? "Unknown")}", maxDepth);
		}

		/// <summary>A helper to log exception stack trace in a more readable way.</summary>
		/// <param name="ex"></param>
		/// <param name="delegateName">reference for method's name or any other message.</param>
		/// <param name="maxDepth">-1 mean no limit</param>
		public static void DeepLogInvocationException(this Exception ex, string delegateName, int maxDepth = -1)
		{
			int depth = 0;
			Exception orgEx = ex;
			List<Exception> exStack = new List<Exception>(Mathf.Max(maxDepth, 2));
			while (ex != null && ex.InnerException != null &&
				(depth++ < maxDepth || maxDepth == -1))
			{
				exStack.Add(ex);
				ex = ex.InnerException;
			}

			// Fall back when no exception was logged
			if (exStack.Count == 0)
			{
				if (TryGetException(orgEx, out var stackTraceDetail))
				{
					UnityEngine.Debug.LogError($"{orgEx.GetType().Name} during \"{delegateName}\" > \"{orgEx.Message}\"\n\n{stackTraceDetail}\n-EOF\n");
				}
				else
				{
					UnityEngine.Debug.LogError($"{orgEx.GetType().Name} during \"{delegateName}\" > \"{orgEx.Message}\"\n\n{orgEx.StackTrace}\n-EOF\n");
				}
			}
			else
			{
				PrintInnerException(exStack);
			}

			void PrintInnerException(List<Exception> exStack)
			{
				int i = exStack.Count;
				while (i-- > 0)
				{
					var ev2 = exStack[i];
					if (TryGetException(ev2, out var stackTraceDetail))
					{
						UnityEngine.Debug.LogError($"{ev2.GetType().Name}[{exStack.Count - i}] \"{delegateName}\" > \"{ev2.Message}\"\n\n{stackTraceDetail}\n");
					}
					else
					{
						UnityEngine.Debug.LogError($"{ev2.GetType().Name}[{exStack.Count - i}] \"{delegateName}\" > \"{ev2.Message}\"\n\n{ex.StackTrace}\n");
					}
				}
			}

			bool TryGetException(Exception exception, out string info)
			{
				StringBuilder sb = new StringBuilder();
				StackTrace trace = new(exception, true);
				for (var k = 0; k < trace.FrameCount; ++k)
				{
					if (TryGetFrameInfo(trace.GetFrame(k), out var line))
					{
						sb.AppendLine(line);
					}
				}
				info = sb.ToString();
				return info.Length > 0;
			}

			bool TryGetFrameInfo(StackFrame frame, out string info)
			{
				info = null;
				if (frame == null)
					return false;
				var filePath = frame.GetFileName();
				if (filePath == null || filePath.Length == 0)
					return false;
				var fileName	= System.IO.Path.GetFileName(filePath);
				var fullDir		= System.IO.Path.GetDirectoryName(filePath);
				var buildInScriptIdx = fullDir.LastIndexOf("Assets");
				var shortDir	= buildInScriptIdx < 0 ? $"../{fileName}" : fullDir.Substring(buildInScriptIdx);
				var lineNo		= frame.GetFileLineNumber();
				var methodLong	= frame.GetMethod().Name;
				var a0			= methodLong.IndexOf('<');
				var a1			= methodLong.IndexOf('>');
				var shortName	= a0 != 1 && a1 != -1 ? methodLong.Substring(a0 + 1, a1 - a0 - 1) : methodLong;
				var lineStr		= $"{shortDir}:{lineNo}";

				info = $"{fileName}:{Color.yellow.ToRichText(shortName)}() (at {lineStr.Hyperlink(filePath, lineNo)})";
				return true;
			}
		}

		#endregion Exception

        public struct ProfilerScope : IDisposable
        {
            private bool m_Disposed;
            public ProfilerScope(string name)
            {
                m_Disposed = false;
                Profiler.BeginSample(name);
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;

                m_Disposed = true;
                Profiler.EndSample();
            }
        }
    }
}