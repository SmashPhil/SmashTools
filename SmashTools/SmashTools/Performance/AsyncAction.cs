using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Performance
{
	public class AsyncAction
	{
		internal Action action;
		internal Func<bool> validator;
		internal Action<Exception> exceptionHandler;

		[Obsolete("Don't instantiate AsyncActions manually, use AsyncPool to retrieve one instead.", error: true)]
		public AsyncAction()
		{
		}

		public void Set(Action action, Func<bool> validator = null, Action<Exception> exceptionHandler = null)
		{
			this.action = action;
			this.validator = validator;
			this.exceptionHandler = exceptionHandler;
		}

		public bool IsValid => validator is null || validator();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Invoke() => action.Invoke();

		public override string ToString()
		{
			return action.Method.Name;
		}
	}
}
