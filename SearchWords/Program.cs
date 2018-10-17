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
		private static Queue _queue;

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
				_queue = new Queue(AddWord);

				if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
				{
					Console.WriteLine("The path to the directory is not correctly specified");
				}
				else
				{
					var fileNames = Directory.GetFiles(path, "*.txt");

					var tasks = new Task[fileNames.Length];

					for (var i = 0; i < fileNames.Length; i++)
					{
						tasks[i] = ProcessFile(fileNames[i]);
					}

					Task.WaitAll(tasks);

					_queue.Dispose();

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
			ThreadPool.SetMinThreads(Environment.ProcessorCount, completionPortThreads);
			ThreadPool.SetMaxThreads(Environment.ProcessorCount, completionPortThreads);
		}

		public static Task ProcessFile(string fileName)
		{
			var task = new Task(() =>
			{
				using (var streammReader = new StreamReader(fileName))
				{
					string line;
					var tempDic = new Dictionary<string, int>();
					while ((line = streammReader.ReadLine()) != null)
					{
						var words = _regex.Matches(line)
							.OfType<Match>()
							.Select(m => m.Groups[0].Value);
						foreach (var word in words)
						{
							if (tempDic.ContainsKey(word))
							{
								tempDic[word]++;
							}
							else
							{
								tempDic.Add(word, 1);
							}
						}
					}
					_queue.Enqueue(tempDic);
				}
			});

			task.Start();

			return task;
		}

		private static void AddWord(Dictionary<string, int> wordsDictionary)
		{
			foreach (var wordPair in wordsDictionary)
			{
				if (WordList.ContainsKey(wordPair.Key))
				{
					WordList[wordPair.Key] += wordPair.Value;
				}
				else
				{
					WordList.Add(wordPair.Key, wordPair.Value);
				}
			}
		}
	}
}
