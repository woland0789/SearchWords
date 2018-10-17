using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SearchWords
{
	public class Queue : IDisposable
	{
		readonly EventWaitHandle _wh = new AutoResetEvent(false);
		private readonly Queue<Dictionary<string, int>> _queue = new Queue<Dictionary<string, int>>();
		private readonly Task _taskWorker;
		private readonly Action<Dictionary<string, int>> _action;

		public void Dispose()
		{
			Enqueue(null);
			_taskWorker.Wait();
			_wh.Close();
		}

		public Queue(Action<Dictionary<string, int>> action)
		{
			_action = action;
			_taskWorker = new Task(Work);
			_taskWorker.Start();
		}

		public void Enqueue(Dictionary<string, int> wordsDictionary)
		{
			_queue.Enqueue(wordsDictionary);
			_wh.Set();
		}

		private void Work()
		{
			while (true)
			{
				try
				{
					var task = _queue.Dequeue();
					if (task == null)
						return;

					_action(task);
				}
				catch (InvalidOperationException ex)
				{
					_wh.WaitOne();
				}
			}
		}
	}
}
