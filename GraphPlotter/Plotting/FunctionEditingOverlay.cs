using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;

namespace GraphPlotter.Plotting
{
	class FunctionEditingOverlay : Canvas
	{
		public readonly PlotCanvas Plot;

		public FunctionEditingOverlay(PlotCanvas plot)
		{
			Plot = plot;


			var b = new Button("Hello");
			AddChild(b, 30, 30);
		}
	}
}
