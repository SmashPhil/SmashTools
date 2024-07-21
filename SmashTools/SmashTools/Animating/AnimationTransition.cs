using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SmashTools.Animations
{
	public class AnimationTransition : IXmlExport
	{
		public AnimationState fromState;
		public AnimationState toState;

		void IXmlExport.Export()
		{
			throw new NotImplementedException();
		}

		//needs conditions

		//needs exit time
	}
}
