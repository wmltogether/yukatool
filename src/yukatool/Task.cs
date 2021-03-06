﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Yuka {
	abstract class Task {
		public static Task currentTask;
		public static Task defaultTask;
		public static Dictionary<string, Task> registeredTasks = new Dictionary<string, Task>();

		public abstract Task NewTask();
		public abstract void DefaultFlags(FlagCollection flags);
		protected abstract void Execute();

		public Task callingTask;
		public string currentFile = "";

		public FlagCollection flags;
		public string[] arguments;

		public Task() {
		}

		public Task(string[] arguments, FlagCollection flags) {
			this.arguments = arguments;
			this.flags = flags;
		}

		public void Run() {
			FlagCollection parentFlags = FlagCollection.current;
			FlagCollection.current = flags;
			callingTask = currentTask;
			currentTask = this;
			Execute();
			currentTask = callingTask;
			FlagCollection.current = parentFlags;
		}

		[DebuggerStepThrough]
		public void Fail(string message) {
			throw new Exception(message);
		}

		[DebuggerStepThrough]
		public void Log(string message) {
			if(flags.Has('v')) {
				Console.WriteLine(message);
			}
		}

		[DebuggerStepThrough]
		public void Log(string message, ConsoleColor color) {
			if(flags.Has('v')) {
				ConsoleColor old = Console.ForegroundColor;
				Console.ForegroundColor = color;
				Console.WriteLine(message);
				Console.ForegroundColor = old;
			}
		}

		public static Task Create(string[] args) {
			Task task = null;
			List<string> param = new List<string>();
			List<object> flagNames = new List<object>();

			foreach(string arg in args) {

				// Flag names
				if(arg.StartsWith("--")) {
					flagNames.Add(arg.Substring(2));
				}

				// Flag codes
				else if(arg.StartsWith("-")) {
					foreach(char code in arg.Substring(1)) {
						flagNames.Add(code);
					}
				}

				// Task name
				else if(task == null && registeredTasks.ContainsKey(arg)) {
					task = registeredTasks[arg];
				}

				// Task parameters
				else {
					param.Add(arg);
				}
			}

			if(task == null) {
				task = defaultTask;
			}

			FlagCollection flags = new FlagCollection();
			task.DefaultFlags(flags);

			foreach(object flag in flagNames) {
				if(flag is char) {
					flags.Set((char)flag);
				}
				else {
					flags.Set((string)flag);
				}
			}

			task = task.NewTask();
			task.flags = flags;
			task.arguments = param.ToArray();

			return task;
		}

		public static void Register(string name, Task task) {
			registeredTasks[name] = task;
		}

		public static void SetDefault(string name) {
			if(registeredTasks.ContainsKey(name)) {
				defaultTask = registeredTasks[name];
			}
		}
	}
}
