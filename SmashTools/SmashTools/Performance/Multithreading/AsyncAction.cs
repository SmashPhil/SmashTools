using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Performance
{
	public abstract class AsyncAction
	{
		public virtual bool IsValid => true;

		public abstract void Invoke();

		public abstract void ReturnToPool();

		public virtual void ExceptionThrown(Exception ex)
		{
		}
	}
}
