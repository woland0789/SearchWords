using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SearchWords
{
	class Program
	{
		private static Dictionary<string, int> WordList { get; set; }
		private static Regex _regex;
		private static int _wordLength;

		static void Main(string[] args)
		{
			var path = ConfigurationManager.AppSettings["Path"];

			ThreadPoolInit();

			if (!int.TryParse(ConfigurationManager.AppSettings["Length"], out _wordLength))
			{
				Console.WriteLine("The Length parameter in the configuration file is invalid");
			}
			else
			{
				WordList = new Dictionary<string, int>();
				_regex = new Regex(@"\w{" + _wordLength + ",}");

				if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
				{
					Console.WriteLine("The path to the directory is not correctly specified");
				}
				else
				{
					var fileNames = Directory.GetFiles(path, "*.txt");

					var tasks = new Task<Dictionary<string, int>>[fileNames.Length];

					for (var i = 0; i < fileNames.Length; i++)
					{
						tasks[i] = ProcessFile(fileNames[i]);
					}

					var task = Task.WhenAll(tasks);
					foreach (var words in task.Result)
					{
						foreach (var word in words)
						{
							if (WordList.ContainsKey(word.Key))
							{
								WordList[word.Key] += word.Value;
							}
							else
							{
								WordList.Add(word.Key, word.Value);
							}
						}
					}

					foreach (var j in WordList.OrderByDescending(x => x.Value).Take(10))
					{
						Console.WriteLine($"{j.Key} : {j.Value}");
					}
				}
			}

			Console.WriteLine("");
			Console.WriteLine("Process done");
			Console.ReadKey();
		}

		private static void ThreadPoolInit()
		{
			ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
			ThreadPool.SetMinThreads(4, completionPortThreads);
			ThreadPool.SetMaxThreads(4, completionPortThreads);
		}

		public static Task<Dictionary<string, int>> ProcessFile(string fileName)
		{
			var task = new Task<Dictionary<string, int>>(() =>
			{
				var result = new Dictionary<string, int>();
				using (var streammReader = new StreamReader(fileName))
				{
					string line;
					while ((line = streammReader.ReadLine()) != null)
					{
						var words = _regex.Matches(line)
							.OfType<Match>()
							.Select(m => m.Groups[0].Value);
						foreach (var word in words)
						{
							if (result.ContainsKey(word))
							{
								result[word]++;
							}
							else
							{
								result.Add(word, 1);
							}
						}
					}
				}

				return result;
			});

			task.Start();

			return task;
		}
	}
}
