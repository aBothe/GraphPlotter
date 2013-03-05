using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;

namespace GraphPlotter.Plotting
{
	class FunctionInputDialog : Dialog
	{
		public readonly Function Function;

		public FunctionInputDialog(Function f = null)
		{
			Function = f ?? new Function();

			this.Buttons.Add(Command.Ok);
			this.Buttons.Add(Command.Cancel);
		}
	}
}
