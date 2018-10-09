using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SearchWords
{
	class Program
	{
		private static Dictionary<string, int> WordList { get; set; }
		private static readonly Regex Regex = new Regex(@"\w*[-|\w]\w*");
		private static int _wordLength;

		static void Main(string[] args)
		{
			var path = ConfigurationManager.AppSettings["Path"];

			if (!int.TryParse(ConfigurationManager.AppSettings["Length"], out _wordLength))
			{
				Console.WriteLine("The Length parameter in the configuration file is invalid");
			}
			else
			{
				WordList = new Dictionary<string, int>();

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

		public static Task ProcessFile(string fileName)
		{
			var task = new Task(() =>
			{
				using (var streammReader = new StreamReader(fileName))
				{
					string line;
					while ((line = streammReader.ReadLine()) != null)
					{
						var words = Regex.Matches(line)
							.OfType<Match>()
							.Select(m => m.Groups[0].Value)
							.Where(x => x.Length >= _wordLength)
							.ToArray();
						foreach (var word in words)
						{
							lock (WordList)
							{
								if (WordList.ContainsKey(word))
								{
									WordList[word]++;
								}
								else
								{
									WordList.Add(word, 1);
								}
							}
						}
					}
				}
			});

			task.Start();

			return task;
		}
	}
}
